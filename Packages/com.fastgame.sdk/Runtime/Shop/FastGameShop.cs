using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;

namespace FastGame
{
    public sealed class FastGameShop
    {
        readonly FastGameHttp _http;
        readonly FastGameConfig _config;

        public FastGameShop(FastGameHttp http, FastGameConfig config)
        {
            _http = http;
            _config = config;
        }

        public bool HasPendingPayment => !string.IsNullOrEmpty(PlayerPrefs.GetString(_config.PendingPaymentPrefsKey, ""));

        /// <summary>Last shop access / restore / unlock error for UI (no backend logs needed).</summary>
        public string LastShopMessage { get; private set; } = "";

        public FastGameShopProgress LastShopProgress { get; private set; } = FastGameShopProgress.Failed;

        /// <summary>
        /// Freeze the current Myket/Cafe Bazaar/Play wallet for this Fast Game user.
        /// Call at login so mid-game store-account switches cannot restore another wallet.
        /// </summary>
        public async Task BindStoreLockAsync()
        {
            string provider;
            string gameCode;
            try
            {
                provider = ResolveProvider(null);
                gameCode = ResolveGameCode(null);
            }
            catch (FastGameException)
            {
                return;
            }
            if (!IsAndroidStoreProvider(provider))
                return;
            try
            {
                var body = new Dictionary<string, object>
                {
                    { "game_code", gameCode },
                    { "provider", provider },
                };
                await _http.RequestRawAsync(
                    "POST",
                    "/apps/games/shop/store-lock",
                    FastGameJson.Stringify(body));
            }
            catch (Exception)
            {
            }
            try
            {
                await EnsureStoreVerifyKeyAsync();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Fetch Cafe Bazaar / Myket RSA from Editor payment config (FG1-wrapped).
        /// Skips when Initialize Game already set a key. Never requests JWT / api_secret.
        /// </summary>
        public async Task EnsureStoreVerifyKeyAsync()
        {
            if (!string.IsNullOrEmpty(_config.StorePublicKey))
            {
                FastGameStore.StorePublicKey = _config.StorePublicKey;
                return;
            }
            if (!string.IsNullOrEmpty(FastGameStore.StorePublicKey))
            {
                _config.StorePublicKey = FastGameStore.StorePublicKey;
                return;
            }
            string provider;
            string gameCode;
            try
            {
                provider = ResolveProvider(null);
                gameCode = ResolveGameCode(null);
            }
            catch (FastGameException)
            {
                return;
            }
            if (!FastGameStoreVerify.NeedsRemoteRsa(provider))
                return;

            var path =
                $"/apps/games/catalog/{UnityEngine.Networking.UnityWebRequest.EscapeURL(gameCode)}/store-verify-key"
                + $"?provider={UnityEngine.Networking.UnityWebRequest.EscapeURL(provider)}";
            var text = await _http.RequestRawAsync("GET", path);
            var o = FastGameJson.ParseObject(text);
            if (!FastGameJson.GetBool(o, "configured"))
                throw new FastGameException(
                    provider + " RSA public key is not set in Fast Game Editor payment config");
            var pem = FastGameStoreVerify.Unwrap(
                FastGameJson.GetString(o, "rsa_verify_key"),
                gameCode,
                provider);
            if (string.IsNullOrEmpty(pem))
                throw new FastGameException("FastGame: store RSA decode failed");
            _config.StorePublicKey = pem;
            FastGameStore.StorePublicKey = pem;
        }

        public async Task<List<ShopLine>> GetCatalogAsync(
            string gameId = null,
            string lang = null,
            bool expandI18n = false)
        {
            gameId = ResolveGameCode(gameId);
            var path =
                $"/apps/games/shop/catalog?game_code={UnityEngine.Networking.UnityWebRequest.EscapeURL(gameId)}";
            path = FastGameHttp.AppendI18nQuery(path, lang, expandI18n);
            var text = await _http.RequestRawAsync("GET", path);
            var arr = FastGameJson.ParseArray(text) ?? new List<object>();
            var list = new List<ShopLine>();
            foreach (var item in arr)
            {
                var line = FastGameDto.ParseShopLine(item as Dictionary<string, object>);
                if (line == null) continue;
                if (!string.Equals(line.GameCode, gameId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                list.Add(line);
            }
            return list;
        }

        public string ResolveGameCode(string gameCode)
        {
            var code = (gameCode ?? "").Trim();
            if (string.IsNullOrEmpty(code))
                code = (_config.GameCode ?? "").Trim();
            if (string.IsNullOrEmpty(code))
                throw new FastGameException(
                    "FastGame: GameCode not set — call Initialize Game");
            return code;
        }

        public string ResolveProvider(string provider)
        {
            var id = FastGameConfig.NormalizeProviderId(provider);
            if (string.IsNullOrEmpty(id))
                id = FastGameConfig.NormalizeProviderId(_config.StorePlatform);
            if (string.IsNullOrEmpty(id))
                throw new FastGameException(
                    "FastGame: StorePlatform not set — call Initialize Game");
            return id;
        }

        public async Task ClaimFreeAsync(string gameCode, string skuKind, string skuId)
        {
            var body = new Dictionary<string, object>
            {
                { "game_code", ResolveGameCode(gameCode) },
                { "sku_kind", skuKind },
                { "sku_id", skuId },
            };
            await _http.RequestRawAsync("POST", "/apps/games/shop/claim-free", FastGameJson.Stringify(body));
        }

        /// <summary>
        /// Redeem an assigned free_unlock discount code (grants ownership).
        /// </summary>
        public async Task RedeemCodeAsync(string gameCode, string code)
        {
            var body = new Dictionary<string, object>
            {
                { "game_code", ResolveGameCode(gameCode) },
                { "code", code },
            };
            await _http.RequestRawAsync("POST", "/apps/games/shop/redeem-code", FastGameJson.Stringify(body));
        }

        /// <summary>
        /// Content lock + player ownership for a shop SKU (storage NAME ids).
        /// Android store: also queries native inventory (no purchase UI). If the store already
        /// owns the SKU, completes Unlock so Fast Game ownership matches. ZarinPal/Steam: Fast Game only.
        /// </summary>
        public async Task<(bool Locked, bool Owned)> GetShopSkuAccessAsync(
            string gameCode,
            string skuKind,
            string skuId)
        {
            LastShopMessage = "";
            gameCode = ResolveGameCode(gameCode);
            await BindStoreLockAsync();
            try
            {
                await EnsureStoreVerifyKeyAsync();
            }
            catch (Exception)
            {
            }
            var provider = "";
            try
            {
                provider = ResolveProvider(null);
            }
            catch (FastGameException)
            {
                provider = "";
            }
            var path =
                $"/apps/games/shop/access?game_code={UnityEngine.Networking.UnityWebRequest.EscapeURL(gameCode)}"
                + $"&sku_kind={UnityEngine.Networking.UnityWebRequest.EscapeURL(skuKind)}"
                + $"&sku_id={UnityEngine.Networking.UnityWebRequest.EscapeURL(skuId)}";
            if (!string.IsNullOrEmpty(provider))
                path += $"&provider={UnityEngine.Networking.UnityWebRequest.EscapeURL(provider)}";
            var text = await _http.RequestRawAsync("GET", path);
            var o = FastGameJson.ParseObject(text);
            var locked = FastGameJson.GetBool(o, "locked");
            var owned = FastGameJson.GetBool(o, "owned");
            if (owned)
                return (locked, true);

            var storeProductIds = ParseStoreProductIds(o);
            if (storeProductIds.Count == 0 || !IsAndroidStoreProvider(provider))
                return (locked, owned);

            try
            {
                if (await TrySyncStoreOwnershipAsync(
                        gameCode, skuKind, skuId, storeProductIds, provider))
                    return (locked, true);
            }
            catch (FastGameException ex)
            {
                LastShopMessage = FastGameJson.ParseApiErrorMessage(ex.Message);
            }
            catch (Exception ex)
            {
                LastShopMessage = ex.Message;
            }
            if (!string.IsNullOrEmpty(LastShopMessage))
                Debug.LogWarning("FastGame shop access: " + LastShopMessage);
            return (locked, owned);
        }

        static List<string> ParseStoreProductIds(Dictionary<string, object> o)
        {
            var ids = new List<string>();
            if (o != null && o.TryGetValue("store_product_ids", out var raw) && raw is IList list)
            {
                foreach (var item in list)
                {
                    var id = (item?.ToString() ?? "").Trim();
                    if (!string.IsNullOrEmpty(id)
                        && !ids.Exists(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
                        ids.Add(id);
                }
            }
            var single = FastGameJson.GetString(o, "store_product_id");
            if (!string.IsNullOrWhiteSpace(single)
                && !ids.Exists(x => string.Equals(x, single.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                ids.Insert(0, single.Trim());
            }
            return ids;
        }

        static bool IsAndroidStoreProvider(string provider)
        {
            var id = FastGameConfig.NormalizeProviderId(provider);
            return id == "myket" || id == "caffebazar" || id == "googleplay";
        }

        public static FastGameShopProgress ClassifyProgress(bool owned, bool pending, bool ok, string message)
        {
            var lower = (message ?? "").ToLowerInvariant();
            if (lower.Contains("is not installed") || lower.Contains("store missing") || lower.Contains("plugin not loaded"))
                return FastGameShopProgress.StoreMissing;
            if (lower.Contains("cancel"))
                return FastGameShopProgress.Cancelled;
            if (owned) return FastGameShopProgress.Success;
            if (pending) return FastGameShopProgress.Pending;
            if (!ok) return FastGameShopProgress.Failed;
            return FastGameShopProgress.Success;
        }

        void SetProgress(FastGameShopProgress progress, string message)
        {
            LastShopProgress = progress;
            if (!string.IsNullOrEmpty(message))
                LastShopMessage = message;
        }

        /// <summary>
        /// After returning from ZarinPal / store. Completes a pending checkout when one exists.
        /// </summary>
        public async Task<(FastGameShopProgress Progress, bool Owned, string Message)> CheckShopProgressAsync()
        {
            string provider;
            try { provider = ResolveProvider(null); }
            catch (FastGameException e)
            {
                SetProgress(LastShopProgress, e.Message);
                return (LastShopProgress, false, e.Message);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsAndroidStoreProvider(provider) && !FastGameStore.IsStoreAppInstalled(provider))
            {
                var missing = FastGameConfig.StoreNotInstalledMessage(provider);
                SetProgress(FastGameShopProgress.StoreMissing, missing);
                return (FastGameShopProgress.StoreMissing, false, missing);
            }
#endif
            if (HasPendingPayment)
            {
                try
                {
                    var done = await CompleteUnlockAsync();
                    var progress = ClassifyProgress(done.Owned || done.Success, false, done.Success, done.Message);
                    SetProgress(progress, done.Message);
                    return (progress, done.Owned || done.Success, done.Message ?? "");
                }
                catch (FastGameException e)
                {
                    var progress = ClassifyProgress(false, false, false, e.Message);
                    if ((e.Message ?? "").IndexOf("No pending", StringComparison.OrdinalIgnoreCase) >= 0)
                        progress = LastShopProgress;
                    SetProgress(progress, e.Message);
                    return (progress, false, e.Message);
                }
            }
            return (LastShopProgress, LastShopProgress == FastGameShopProgress.Success, LastShopMessage);
        }

        static readonly HashSet<string> NativeInventoryAttempted =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        async Task<bool> TrySyncStoreOwnershipAsync(
            string gameCode,
            string skuKind,
            string skuId,
            IReadOnlyList<string> storeProductIds,
            string provider)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!FastGameStore.IsStoreAppInstalled(provider))
                return false;
            foreach (var storeProductId in storeProductIds)
            {
                var trimmed = (storeProductId ?? "").Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                lock (NativeInventoryAttempted)
                {
                    if (!NativeInventoryAttempted.Add(trimmed))
                        continue;
                }
                var native = await FastGameStore.PurchaseOrRestoreAsync(trimmed, false);
                if (string.IsNullOrWhiteSpace(native.PurchaseToken))
                    continue;

                var body = new Dictionary<string, object>
                {
                    { "game_code", gameCode },
                    { "sku_kind", skuKind },
                    { "sku_id", skuId },
                    { "provider", provider },
                    { "purchase_token", native.PurchaseToken.Trim() },
                    { "store_product_id", trimmed },
                };
                string text;
                try
                {
                    text = await _http.RequestRawAsync(
                        "POST",
                        "/apps/games/shop/unlock/restore",
                        FastGameJson.Stringify(body));
                }
                catch (FastGameException ex)
                {
                    LastShopMessage = FastGameJson.ParseApiErrorMessage(ex.Message);
                    continue;
                }
                var o = FastGameJson.ParseObject(text);
                if (FastGameJson.GetBool(o, "owned") || FastGameJson.GetBool(o, "success"))
                    return true;
                var clientMsg = FastGameJson.ExtractShopUnlockMessage(
                    o, "Store restore validation failed");
                if (!string.IsNullOrWhiteSpace(clientMsg))
                    LastShopMessage = clientMsg;
            }
            return false;
#else
            await Task.CompletedTask;
            return false;
#endif
        }

        /// <summary>
        /// One purchase flow. Empty gameCode → Initialize Game. Provider = StorePlatform.
        /// Store IAP: begin → native token → complete. ZarinPal/Steam: begin then CompleteUnlockAsync.
        /// </summary>
        public async Task<ShopUnlockResult> UnlockSkuAsync(
            string gameCode,
            string skuKind,
            string skuId,
            string callbackUrl = null,
            string discountCode = null)
        {
            gameCode = ResolveGameCode(gameCode);
            await BindStoreLockAsync();
            var provider = ResolveProvider(null);
            try
            {
                await EnsureStoreVerifyKeyAsync();
            }
            catch (FastGameException)
            {
                if (FastGameStoreVerify.NeedsRemoteRsa(provider)
                    && string.IsNullOrEmpty(_config.StorePublicKey)
                    && string.IsNullOrEmpty(FastGameStore.StorePublicKey))
                    throw;
            }
            var body = new Dictionary<string, object>
            {
                { "game_code", gameCode },
                { "sku_kind", skuKind },
                { "sku_id", skuId },
                { "provider", provider },
            };
            if (!string.IsNullOrEmpty(callbackUrl))
                body["callback_url"] = callbackUrl;
            if (!string.IsNullOrEmpty(discountCode))
                body["discount_code"] = discountCode;

            var text = await _http.RequestRawAsync(
                "POST",
                "/apps/games/shop/unlock/begin",
                FastGameJson.Stringify(body));
            var o = FastGameJson.ParseObject(text) ?? new Dictionary<string, object>();
            var result = ParseUnlock(o, provider);
            if (result.Pending && !string.IsNullOrEmpty(result.Authority) && !string.IsNullOrEmpty(result.PaymentToken))
            {
                SavePending(new PaymentInitiateResult
                {
                    Authority = result.Authority,
                    PaymentToken = result.PaymentToken,
                    PaymentUrl = result.PaymentUrl,
                    StoreProductId = result.StoreProductId,
                    OrderId = result.OrderId,
                    Amount = result.Amount,
                    Provider = result.Provider ?? provider,
                }, new List<object>
                {
                    new Dictionary<string, object>
                    {
                        { "game_code", gameCode },
                        { "sku_kind", skuKind },
                        { "sku_id", skuId },
                    },
                }, result.Provider ?? provider);
            }
            if (!string.IsNullOrEmpty(result.PaymentUrl))
                Application.OpenURL(result.PaymentUrl);

            if (result.Owned || !result.Pending)
            {
                SetProgress(ClassifyProgress(result.Owned, result.Pending, true, ""), "");
                return result;
            }

            var mode = (result.Mode ?? "").Trim().ToLowerInvariant();
            var isStore = mode == "store"
                || provider == "myket" || provider == "caffebazar" || provider == "googleplay";
            if (!isStore)
            {
                SetProgress(FastGameShopProgress.Pending, "");
                return result;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!FastGameStore.IsStoreAppInstalled(provider))
                throw new FastGameException(FastGameConfig.StoreNotInstalledMessage(provider));
            if (string.IsNullOrEmpty(result.StoreProductId))
                throw new FastGameException("StoreProductId empty — map store_skus in Fast Game");
            if (FastGameStoreVerify.NeedsRemoteRsa(provider)
                && string.IsNullOrEmpty(FastGameStore.StorePublicKey)
                && string.IsNullOrEmpty(_config.StorePublicKey))
            {
                throw new FastGameException(
                    provider + " RSA public key is not set in Fast Game Editor payment config");
            }
            var native = await FastGameStore.PurchaseOrRestoreAsync(result.StoreProductId, true);
            if (string.IsNullOrWhiteSpace(native.PurchaseToken))
            {
                if (!FastGameStore.IsStoreAppInstalled(provider))
                    throw new FastGameException(FastGameConfig.StoreNotInstalledMessage(provider));
                throw new FastGameException("Purchase cancelled.");
            }
            var done = await CompleteUnlockAsync(native.PurchaseToken);
            result.Success = done.Success;
            result.Owned = done.Owned || done.Success;
            result.Pending = false;
            SetProgress(ClassifyProgress(result.Owned, false, done.Success, done.Message), done.Message);
            return result;
#else
            SetProgress(FastGameShopProgress.Pending, "");
            return result;
#endif
        }

        public async Task<PaymentVerifyResult> CompleteUnlockAsync(string purchaseToken = null)
        {
            var raw = PlayerPrefs.GetString(_config.PendingPaymentPrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                throw new FastGameException("No pending payment");
            var pending = FastGameJson.ParseObject(raw);
            var auth = FastGameJson.GetString(pending, "authority");
            var token = FastGameJson.GetString(pending, "payment_token");
            var body = new Dictionary<string, object>
            {
                { "authority", auth },
                { "payment_token", token },
            };
            if (!string.IsNullOrWhiteSpace(purchaseToken))
                body["purchase_token"] = purchaseToken.Trim();

            var text = await _http.RequestRawAsync(
                "POST",
                "/apps/games/shop/unlock/complete",
                FastGameJson.Stringify(body));
            var o = FastGameJson.ParseObject(text);
            var success = FastGameJson.GetBool(o, "success");
            var owned = FastGameJson.GetBool(o, "owned") || success;
            var message = FastGameJson.ExtractShopUnlockMessage(o, "");
            if (success)
            {
                PlayerPrefs.DeleteKey(_config.PendingPaymentPrefsKey);
                PlayerPrefs.Save();
            }
            else if (!string.IsNullOrWhiteSpace(message))
            {
                LastShopMessage = message;
                Debug.LogWarning("FastGame shop complete: " + message);
            }
            return new PaymentVerifyResult { Success = success, Owned = owned, Message = message };
        }

        /// <summary>Deprecated — use <see cref="UnlockSkuAsync"/>.</summary>
        public async Task<bool> PurchaseOrRestoreStoreSkuAsync(
            string gameCode,
            string skuKind,
            string skuId,
            string provider = null)
        {
            var result = await UnlockSkuAsync(gameCode, skuKind, skuId);
            return result.Owned;
        }

        static ShopUnlockResult ParseUnlock(Dictionary<string, object> o, string fallbackProvider)
        {
            return new ShopUnlockResult
            {
                Owned = FastGameJson.GetBool(o, "owned"),
                Pending = FastGameJson.GetBool(o, "pending"),
                Locked = FastGameJson.GetBool(o, "locked"),
                Mode = FastGameJson.GetString(o, "mode"),
                Provider = FastGameJson.GetString(o, "provider") ?? fallbackProvider,
                Authority = FastGameJson.GetString(o, "authority"),
                PaymentToken = FastGameJson.GetString(o, "payment_token"),
                PaymentUrl = FastGameJson.GetString(o, "payment_url"),
                StoreProductId = FastGameJson.GetString(o, "store_product_id"),
                OrderId = FastGameJson.GetString(o, "order_id"),
                Amount = FastGameJson.GetInt(o, "amount"),
                Currency = FastGameJson.GetString(o, "currency"),
            };
        }

        /// <summary>Obsolete — use <see cref="UnlockSkuAsync"/>.</summary>
        [Obsolete("Use UnlockSkuAsync.")]
        public async Task<PaymentInitiateResult> BuyAsync(ShopLine line, string callbackUrl)
        {
            return await BuyWithProviderAsync(line, callbackUrl, "zarinpal", "rial", null);
        }

        [Obsolete("Use UnlockSkuAsync.")]
        public async Task<PaymentInitiateResult> BuyAsync(ShopLine line, string callbackUrl, string discountCode)
        {
            return await BuyWithProviderAsync(line, callbackUrl, "zarinpal", "rial", discountCode);
        }

        [Obsolete("Use UnlockSkuAsync.")]
        public async Task<PaymentInitiateResult> BuyWithProviderAsync(
            ShopLine line,
            string callbackUrl,
            string provider,
            string currency = "rial")
        {
            return await BuyWithProviderAsync(line, callbackUrl, provider, currency, null);
        }

        [Obsolete("Use UnlockSkuAsync.")]
        public async Task<PaymentInitiateResult> BuyWithProviderAsync(
            ShopLine line,
            string callbackUrl,
            string provider,
            string currency,
            string discountCode)
        {
            if (line == null) throw new FastGameException("Shop line required");
            if (line.Owned) throw new FastGameException("Already owned");
            provider = ResolveProvider(provider);
            var gameCode = ResolveGameCode(line.GameCode);
            // Price is resolved on the server from sku + provider + currency.
            // Free / default items should use ClaimFreeAsync.

            var cart = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "game_code", gameCode },
                    { "sku_kind", line.SkuKind },
                    { "sku_id", line.SkuId },
                },
            };
            var meta = new Dictionary<string, object>
            {
                { "kind", "game_shop" },
                { "lines", cart },
            };

            if (provider == "myket" || provider == "caffebazar" || provider == "googleplay")
            {
                var body = new Dictionary<string, object>
                {
                    { "provider", provider },
                    { "app_scope", "fast-game" },
                    { "purchase_type", "one_time" },
                    { "currency", currency },
                    { "metadata", meta },
                };
                var text = await _http.RequestRawAsync(
                    "POST",
                    "/base/payments/billing/initiate",
                    FastGameJson.Stringify(body));
                var o = FastGameJson.ParseObject(text);
                var result = new PaymentInitiateResult
                {
                    Authority = FastGameJson.GetString(o, "authority"),
                    PaymentUrl = null,
                    PaymentToken = FastGameJson.GetString(o, "payment_token"),
                    Amount = FastGameJson.GetInt(o, "amount"),
                    StoreProductId = FastGameJson.GetString(o, "store_product_id"),
                    Provider = provider,
                };
                SavePending(result, cart, provider);
                return result;
            }

            if (provider == "steam")
            {
                var body = new Dictionary<string, object>
                {
                    { "app_scope", "fast-game" },
                    { "purchase_type", "one_time" },
                    { "currency", currency },
                    { "metadata", meta },
                };
                if (!string.IsNullOrEmpty(discountCode))
                    body["discount_code"] = discountCode;
                var text = await _http.RequestRawAsync(
                    "POST",
                    "/base/payments/steam/initiate",
                    FastGameJson.Stringify(body));
                var o = FastGameJson.ParseObject(text);
                var result = new PaymentInitiateResult
                {
                    Authority = FastGameJson.GetString(o, "authority"),
                    PaymentUrl = null,
                    PaymentToken = FastGameJson.GetString(o, "payment_token"),
                    Amount = FastGameJson.GetInt(o, "amount"),
                    StoreProductId = FastGameJson.GetString(o, "store_product_id"),
                    Provider = "steam",
                    OrderId = FastGameJson.GetString(o, "orderid"),
                };
                SavePending(result, cart, "steam");
                return result;
            }

            var zarinpalBody = new Dictionary<string, object>
            {
                { "provider", "zarinpal" },
                { "app_scope", "fast-game" },
                { "purchase_type", "one_time" },
                { "currency", currency },
                { "callback_url", callbackUrl },
                { "metadata", meta },
            };
            if (!string.IsNullOrEmpty(discountCode))
                zarinpalBody["discount_code"] = discountCode;
            var zarinpalText = await _http.RequestRawAsync(
                "POST",
                "/base/payments/initiate",
                FastGameJson.Stringify(zarinpalBody));
            var zo = FastGameJson.ParseObject(zarinpalText);
            var zresult = new PaymentInitiateResult
            {
                Authority = FastGameJson.GetString(zo, "authority"),
                PaymentUrl = FastGameJson.GetString(zo, "payment_url"),
                PaymentToken = FastGameJson.GetString(zo, "payment_token"),
                Amount = FastGameJson.GetInt(zo, "amount"),
                Provider = "zarinpal",
            };
            SavePending(zresult, cart, "zarinpal");
            if (!string.IsNullOrEmpty(zresult.PaymentUrl))
                Application.OpenURL(zresult.PaymentUrl);
            return zresult;
        }

        [Obsolete("Use CompleteUnlockAsync.")]
        public async Task<PaymentVerifyResult> SubmitBillingAsync(string purchaseToken)
        {
            var raw = PlayerPrefs.GetString(_config.PendingPaymentPrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                throw new FastGameException("No pending payment");
            var pending = FastGameJson.ParseObject(raw);
            var provider = FastGameJson.GetString(pending, "provider") ?? "myket";
            var auth = FastGameJson.GetString(pending, "authority");
            var token = FastGameJson.GetString(pending, "payment_token");
            var lines = FastGameJson.GetArray(pending, "lines");
            var body = new Dictionary<string, object>
            {
                { "provider", provider },
                { "authority", auth },
                { "payment_token", token },
                { "purchase_token", purchaseToken },
                {
                    "metadata", new Dictionary<string, object>
                    {
                        { "kind", "game_shop" },
                        { "lines", lines },
                    }
                },
            };
            var text = await _http.RequestRawAsync(
                "POST",
                "/base/payments/billing/submit",
                FastGameJson.Stringify(body));
            var o = FastGameJson.ParseObject(text);
            PlayerPrefs.DeleteKey(_config.PendingPaymentPrefsKey);
            PlayerPrefs.Save();
            return new PaymentVerifyResult { Success = FastGameJson.GetBool(o, "success") };
        }

        [Obsolete("Use CompleteUnlockAsync.")]
        public async Task<PaymentVerifyResult> FinalizeSteamAsync()
        {
            var raw = PlayerPrefs.GetString(_config.PendingPaymentPrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                throw new FastGameException("No pending payment");
            var pending = FastGameJson.ParseObject(raw);
            var auth = FastGameJson.GetString(pending, "authority");
            var token = FastGameJson.GetString(pending, "payment_token");
            var lines = FastGameJson.GetArray(pending, "lines");
            var body = new Dictionary<string, object>
            {
                { "authority", auth },
                { "payment_token", token },
                {
                    "metadata", new Dictionary<string, object>
                    {
                        { "kind", "game_shop" },
                        { "lines", lines },
                    }
                },
            };
            var text = await _http.RequestRawAsync(
                "POST",
                "/base/payments/steam/finalize",
                FastGameJson.Stringify(body));
            var o = FastGameJson.ParseObject(text);
            PlayerPrefs.DeleteKey(_config.PendingPaymentPrefsKey);
            PlayerPrefs.Save();
            return new PaymentVerifyResult { Success = FastGameJson.GetBool(o, "success") };
        }

        [Obsolete("Use CompleteUnlockAsync.")]
        public async Task<PaymentVerifyResult> VerifyPendingAsync(string authority = null)
        {
            var raw = PlayerPrefs.GetString(_config.PendingPaymentPrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                throw new FastGameException("No pending payment");
            var pending = FastGameJson.ParseObject(raw);
            var provider = FastGameJson.GetString(pending, "provider") ?? "zarinpal";
            if (provider == "myket" || provider == "caffebazar" || provider == "googleplay")
                throw new FastGameException("Use SubmitBillingAsync with the store purchase token");
            if (provider == "steam")
                return await FinalizeSteamAsync();
            var auth = authority ?? FastGameJson.GetString(pending, "authority");
            var token = FastGameJson.GetString(pending, "payment_token");
            var lines = FastGameJson.GetArray(pending, "lines");
            var body = new Dictionary<string, object>
            {
                { "provider", "zarinpal" },
                { "authority", auth },
                { "payment_token", token },
                {
                    "metadata", new Dictionary<string, object>
                    {
                        { "kind", "game_shop" },
                        { "lines", lines },
                    }
                },
            };
            var text = await _http.RequestRawAsync(
                "POST",
                "/base/payments/verify",
                FastGameJson.Stringify(body));
            var o = FastGameJson.ParseObject(text);
            PlayerPrefs.DeleteKey(_config.PendingPaymentPrefsKey);
            PlayerPrefs.Save();
            return new PaymentVerifyResult { Success = FastGameJson.GetBool(o, "success") };
        }

        void SavePending(PaymentInitiateResult result, List<object> cart, string provider)
        {
            var pending = new Dictionary<string, object>
            {
                { "authority", result.Authority },
                { "payment_token", result.PaymentToken },
                { "provider", provider },
                { "lines", cart },
            };
            PlayerPrefs.SetString(_config.PendingPaymentPrefsKey, FastGameJson.Stringify(pending));
            PlayerPrefs.Save();
        }

        public void ClearPendingPayment()
        {
            PlayerPrefs.DeleteKey(_config.PendingPaymentPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
