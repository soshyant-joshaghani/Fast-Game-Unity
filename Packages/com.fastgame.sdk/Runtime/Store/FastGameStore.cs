using System;
using System.Threading.Tasks;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Internal Android store OS (Myket / Cafe Bazaar / Google Play).
    /// Designers use FastGameShop.UnlockSkuAsync — this follows Initialize Game automatically.
    /// Copy exactly one flavor from Plugins/Android/FastGameStore/flavors into src/com/fastgame/store.
    /// </summary>
    public static class FastGameStore
    {
        public const string MyketPackage = "ir.mservices.market";
        public const string CafeBazaarPackage = "com.farsitel.bazaar";
        public const string GooglePlayPackage = "com.android.vending";

        public static string StorePublicKey { get; set; } = "";

        static readonly object Gate = new object();
        static bool ActivityOpen;
        static Task<(string StoreProductId, string PurchaseToken, bool AlreadyOwned)> InFlight;
        static string CachedSku = "";
        static string CachedToken = "";
        static bool CachedOwned;
        static DateTime CachedAt = DateTime.MinValue;
        const double QueryCacheSeconds = 45.0;

        public static string PackageForProvider(string provider)
        {
            var id = FastGameConfig.NormalizeProviderId(provider);
            if (id == "caffebazar") return CafeBazaarPackage;
            if (id == "googleplay") return GooglePlayPackage;
            if (id == "myket") return MyketPackage;
            return "";
        }

        public static bool IsStoreAppInstalled(string provider)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var packageName = PackageForProvider(provider);
            if (string.IsNullOrEmpty(packageName)) return false;
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
                {
                    pm.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>Launch FastGameStoreActivity and wait for purchaseToken (empty = fail). Used by Shop.UnlockSkuAsync.</summary>
        public static Task<(string StoreProductId, string PurchaseToken, bool AlreadyOwned)> RequestPurchaseTokenAsync(
            string storeProductId,
            bool openStorePage = true)
            => PurchaseOrRestoreAsync(storeProductId, openStorePage);

        /// <summary>Launch FastGameStoreActivity and wait for purchaseToken (empty = fail).</summary>
        public static async Task<(string StoreProductId, string PurchaseToken, bool AlreadyOwned)> PurchaseOrRestoreAsync(
            string storeProductId,
            bool openStorePage = true)
        {
            var id = (storeProductId ?? "").Trim();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (string.IsNullOrEmpty(id))
                return ("", "", false);

            Task<(string StoreProductId, string PurchaseToken, bool AlreadyOwned)> waitFor = null;
            lock (Gate)
            {
                if (!openStorePage)
                {
                    if (string.Equals(CachedSku, id, StringComparison.OrdinalIgnoreCase)
                        && (DateTime.UtcNow - CachedAt).TotalSeconds < QueryCacheSeconds)
                    {
                        return (id, CachedToken, CachedOwned);
                    }
                    if (ActivityOpen)
                    {
                        var same = string.Equals(CachedSku, id, StringComparison.OrdinalIgnoreCase);
                        return (id, same ? CachedToken : "", same && CachedOwned);
                    }
                    ActivityOpen = true;
                }
                else if (ActivityOpen)
                {
                    waitFor = InFlight;
                }
                else
                {
                    ActivityOpen = true;
                }
            }

            if (waitFor != null)
            {
                try { await waitFor; }
                catch (Exception) { }
                return await PurchaseOrRestoreAsync(id, true);
            }

            var tcs = new TaskCompletionSource<(string, string, bool)>();
            lock (Gate)
            {
                InFlight = tcs.Task;
            }
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var activityClass = new AndroidJavaClass("com.fastgame.store.FastGameStoreActivity"))
                {
                    var proxy = new StoreListenerProxy((sku, token, already) =>
                    {
                        tcs.TrySetResult((sku ?? "", token ?? "", already));
                    });
                    activityClass.SetStatic("Listener", proxy);

                    using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                    using (var intent = new AndroidJavaObject("android.content.Intent", activity,
                        new AndroidJavaClass("com.fastgame.store.FastGameStoreActivity")))
                    {
                        intent.Call<AndroidJavaObject>("putExtra", "openTheStorePage", openStorePage);
                        if (!string.IsNullOrEmpty(id))
                            intent.Call<AndroidJavaObject>("putExtra", "storeProductId", id);
                        if (!string.IsNullOrEmpty(StorePublicKey))
                            intent.Call<AndroidJavaObject>("putExtra", "storePublicKey", StorePublicKey);
                        activity.Call("startActivity", intent);
                    }
                }
            }
            catch (Exception e)
            {
                lock (Gate)
                {
                    ActivityOpen = false;
                    InFlight = null;
                }
                tcs.TrySetException(e);
            }

            var result = await tcs.Task;
            lock (Gate)
            {
                CachedSku = string.IsNullOrEmpty(result.StoreProductId) ? id : result.StoreProductId;
                CachedToken = result.PurchaseToken ?? "";
                CachedOwned = result.AlreadyOwned || !string.IsNullOrWhiteSpace(result.PurchaseToken);
                CachedAt = DateTime.UtcNow;
                ActivityOpen = false;
                InFlight = null;
            }
            return result;
#else
            throw new FastGameException("FastGameStore: Android only");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        sealed class StoreListenerProxy : AndroidJavaProxy
        {
            readonly Action<string, string, bool> _onDone;

            public StoreListenerProxy(Action<string, string, bool> onDone)
                : base("com.fastgame.store.FastGameStoreActivity$FastGameStoreListener")
            {
                _onDone = onDone;
            }

            public void onStorePurchase(string storeProductId, string purchaseToken, bool alreadyOwned)
            {
                _onDone?.Invoke(storeProductId, purchaseToken, alreadyOwned);
            }
        }
#endif
    }
}
