using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable] public class FastGameShopCatalogEvent : UnityEvent<List<ShopLine>> { }
    [Serializable] public class FastGameShopAccessEvent : UnityEvent<bool, bool> { }
    [Serializable] public class FastGamePaymentEvent : UnityEvent<PaymentInitiateResult> { }
    [Serializable] public class FastGameVerifyEvent : UnityEvent<bool> { }
    [Serializable] public class FastGameShopProgressEvent : UnityEvent<FastGameShopProgress, bool, string> { }

    /// <summary>
    /// Drop on a GameObject for shop access (UE Fast Game | Shop nodes).
    /// Requires login via <see cref="FastGameAuthBehaviour"/> / shared client token.
    /// </summary>
    [AddComponentMenu("Fast Game/Shop")]
    public sealed class FastGameShopBehaviour : MonoBehaviour
    {
        [Header("Client")]
        public FastGameClientBehaviour ClientHost;

        [Header("Shop")]
        [Tooltip("Empty → Initialize Game GameCode")]
        public string GameId = "";
        public string Lang = "";
        public bool ExpandI18n;
        public string PaymentCallbackUrl = "http://dashboard.localhost/panel/playground";
        [Tooltip("Empty → Initialize Game StorePlatform")]
        public string Provider = "";
        public string Currency = "rial";

        [Header("SKU (for Claim / Access / Buy by ids)")]
        public string SkuKind;
        public string SkuId;

        public bool Busy { get; private set; }

        public List<ShopLine> LastCatalog { get; private set; } = new List<ShopLine>();
        public ShopLine SelectedLine { get; private set; }

        [Header("Events")]
        public FastGameShopCatalogEvent OnCatalogLoaded;
        public FastGameShopAccessEvent OnSkuAccess;
        public FastGameAuthResultEvent OnClaimComplete;
        public FastGamePaymentEvent OnBuyStarted;
        public FastGameVerifyEvent OnVerifyComplete;
        public FastGameVerifyEvent OnUnlockComplete;
        public FastGameShopProgressEvent OnShopProgress;
        public UnityEvent OnPurchaseSuccessful;
        public UnityEvent OnPurchasePending;
        public UnityEvent OnPurchaseFailed;
        public UnityEvent OnPurchaseCancelled;
        public UnityEvent OnStoreMissing;
        public FastGameStringEvent OnError;

        FastGameClient Client => FastGameClientBehaviour.RequireClient(ClientHost);

        string ResolvedGameId => Client.Shop.ResolveGameCode(GameId);

        string ResolvedProvider => string.IsNullOrWhiteSpace(Provider)
            ? Client.Shop.ResolveProvider(Provider)
            : FastGameConfig.NormalizeProviderId(Provider);

        public bool HasPendingPayment => Client.Shop.HasPendingPayment;

        public void RefreshCatalog() => _ = Run(RefreshCatalogAsync);

        public async Task RefreshCatalogAsync()
        {
            try
            {
                LastCatalog = await Client.Shop.GetCatalogAsync(
                    ResolvedGameId,
                    string.IsNullOrWhiteSpace(Lang) ? null : Lang,
                    ExpandI18n);
                OnCatalogLoaded?.Invoke(LastCatalog);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        public void GetSkuAccess() => _ = Run(GetSkuAccessAsync);

        public async Task GetSkuAccessAsync()
        {
            try
            {
                var (locked, owned) = await Client.Shop.GetShopSkuAccessAsync(ResolvedGameId, SkuKind, SkuId);
                OnSkuAccess?.Invoke(locked, owned);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        /// <summary>Claim free SKU using SkuKind / SkuId fields.</summary>
        public void ClaimFree() => _ = Run(() => ClaimFreeAsync(ResolvedGameId, SkuKind, SkuId));

        public void ClaimFreeLine(ShopLine line)
        {
            if (line == null) return;
            SelectedLine = line;
            _ = Run(() => ClaimFreeAsync(line.GameCode, line.SkuKind, line.SkuId));
        }

        public async Task ClaimFreeAsync(string gameCode, string skuKind, string skuId)
        {
            try
            {
                var code = string.IsNullOrEmpty(gameCode) ? ResolvedGameId : gameCode;
                await Client.Shop.ClaimFreeAsync(code, skuKind, skuId);
                OnClaimComplete?.Invoke(true, 200, "claimed");
                await RefreshCatalogAsync();
            }
            catch (Exception e)
            {
                OnClaimComplete?.Invoke(false, TryParseStatus(e.Message), e.Message);
                OnError?.Invoke(e.Message);
            }
        }

        /// <summary>Unlock SelectedLine or SkuKind/SkuId (one flow for all platforms).</summary>
        public void UnlockSku() => _ = Run(UnlockSkuAsync);

        public void UnlockSkuLine(ShopLine line)
        {
            SelectedLine = line;
            _ = Run(UnlockSkuAsync);
        }

        public async Task UnlockSkuAsync()
        {
            try
            {
                var line = SelectedLine ?? FindLine(SkuKind, SkuId);
                if (line == null && (string.IsNullOrEmpty(SkuKind) || string.IsNullOrEmpty(SkuId)))
                    throw new FastGameException("No shop line selected");
                var skuKind = line != null ? line.SkuKind : SkuKind;
                var skuId = line != null ? line.SkuId : SkuId;
                var game = line != null ? line.GameCode : ResolvedGameId;
                var result = await Client.Shop.UnlockSkuAsync(game, skuKind, skuId, PaymentCallbackUrl);
                EmitProgress(FastGameShop.ClassifyProgress(result.Owned, result.Pending, true, Client.Shop.LastShopMessage), result.Owned, Client.Shop.LastShopMessage);
                OnUnlockComplete?.Invoke(result.Owned);
                if (!result.Owned && result.Pending && !string.IsNullOrEmpty(result.PaymentUrl))
                {
                    OnBuyStarted?.Invoke(new PaymentInitiateResult
                    {
                        Authority = result.Authority,
                        PaymentUrl = result.PaymentUrl,
                        PaymentToken = result.PaymentToken,
                        Amount = result.Amount,
                        Provider = result.Provider,
                        StoreProductId = result.StoreProductId,
                        OrderId = result.OrderId,
                    });
                }
                if (result.Owned)
                    await RefreshCatalogAsync();
            }
            catch (Exception e)
            {
                OnUnlockComplete?.Invoke(false);
                EmitProgress(FastGameShop.ClassifyProgress(false, false, false, e.Message), false, e.Message);
                OnError?.Invoke(e.Message);
            }
        }

        public void CompleteUnlock() => _ = Run(() => CompleteUnlockAsync(null));

        public async Task CompleteUnlockAsync(string purchaseToken = null)
        {
            try
            {
                var res = await Client.Shop.CompleteUnlockAsync(purchaseToken);
                EmitProgress(FastGameShop.ClassifyProgress(res.Owned || res.Success, false, res.Success, res.Message), res.Owned || res.Success, res.Message);
                OnUnlockComplete?.Invoke(res.Owned || res.Success);
                if (res.Success)
                    await RefreshCatalogAsync();
            }
            catch (Exception e)
            {
                OnUnlockComplete?.Invoke(false);
                EmitProgress(FastGameShop.ClassifyProgress(false, false, false, e.Message), false, e.Message);
                OnError?.Invoke(e.Message);
            }
        }

        public void CheckShopProgress() => _ = Run(CheckShopProgressAsync);

        public async Task CheckShopProgressAsync()
        {
            try
            {
                var (progress, owned, message) = await Client.Shop.CheckShopProgressAsync();
                EmitProgress(progress, owned, message);
                if (progress == FastGameShopProgress.Success)
                    await RefreshCatalogAsync();
            }
            catch (Exception e)
            {
                EmitProgress(FastGameShop.ClassifyProgress(false, false, false, e.Message), false, e.Message);
                OnError?.Invoke(e.Message);
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            var host = ClientHost != null ? ClientHost : FastGameClientBehaviour.Instance;
            if (host == null || host.Client == null || !host.Client.Shop.HasPendingPayment)
                return;
            CheckShopProgress();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause) return;
            OnApplicationFocus(true);
        }

        void EmitProgress(FastGameShopProgress progress, bool owned, string message)
        {
            OnShopProgress?.Invoke(progress, owned, message ?? "");
            switch (progress)
            {
                case FastGameShopProgress.Success:
                    OnPurchaseSuccessful?.Invoke();
                    break;
                case FastGameShopProgress.Pending:
                    OnPurchasePending?.Invoke();
                    break;
                case FastGameShopProgress.Cancelled:
                    OnPurchaseCancelled?.Invoke();
                    break;
                case FastGameShopProgress.StoreMissing:
                    OnStoreMissing?.Invoke();
                    break;
                default:
                    OnPurchaseFailed?.Invoke();
                    break;
            }
            if (progress == FastGameShopProgress.Failed || progress == FastGameShopProgress.StoreMissing || progress == FastGameShopProgress.Cancelled)
            {
                if (!string.IsNullOrEmpty(message))
                    OnError?.Invoke(message);
            }
        }

        /// <summary>Deprecated — use UnlockSku.</summary>
        public void Buy() => _ = Run(BuyAsync);

        public void BuyLine(ShopLine line)
        {
            SelectedLine = line;
            _ = Run(BuyAsync);
        }

        public async Task BuyAsync()
        {
            await UnlockSkuAsync();
        }

        public void VerifyPending() => _ = Run(VerifyPendingAsync);

        public async Task VerifyPendingAsync()
        {
            try
            {
                var res = await Client.Shop.CompleteUnlockAsync();
                OnVerifyComplete?.Invoke(res.Success);
                if (res.Success)
                    await RefreshCatalogAsync();
            }
            catch (Exception e)
            {
                OnVerifyComplete?.Invoke(false);
                OnError?.Invoke(e.Message);
            }
        }

        public void SelectLine(ShopLine line) => SelectedLine = line;

        ShopLine FindLine(string skuKind, string skuId)
        {
            if (LastCatalog == null) return null;
            foreach (var line in LastCatalog)
            {
                if (string.Equals(line.SkuKind, skuKind, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(line.SkuId, skuId, StringComparison.OrdinalIgnoreCase))
                    return line;
            }
            return null;
        }

        static int TryParseStatus(string message)
        {
            if (string.IsNullOrEmpty(message)) return 0;
            var colon = message.IndexOf(':');
            if (colon <= 0) return 0;
            return int.TryParse(message.Substring(0, colon).Trim(), out var code) ? code : 0;
        }

        async Task Run(Func<Task> action)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                await action();
            }
            finally
            {
                Busy = false;
            }
        }
    }
}
