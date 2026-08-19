using System;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable] public class FastGameStoreOwnedEvent : UnityEvent<bool> { }

    /// <summary>
    /// Internal OS helper. Designers should use Fast Game Shop UnlockSku, not this component.
    /// </summary>
    [Obsolete("Use FastGameShopBehaviour.UnlockSku — FastGameStore is internal OS.")]
    public sealed class FastGameStoreBehaviour : MonoBehaviour
    {
        public FastGameClientBehaviour ClientHost;
        public string GameCode = "";
        public string SkuKind = "map";
        public string SkuId = "full_game";
        [Tooltip("Empty → Initialize Game StorePlatform")]
        public string Provider = "";
        public string StorePublicKey = "";

        public FastGameStoreOwnedEvent OnOwned;
        public FastGameStringEvent OnError;

        FastGameClient Client => FastGameClientBehaviour.RequireClient(ClientHost);

        public bool IsStoreAppInstalled()
        {
            var provider = string.IsNullOrWhiteSpace(Provider)
                ? Client.Config.StorePlatform
                : Provider;
            return FastGameStore.IsStoreAppInstalled(provider);
        }

        public void PurchaseOrRestore() => _ = PurchaseOrRestoreAsync();

        public async System.Threading.Tasks.Task PurchaseOrRestoreAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(StorePublicKey))
                    FastGameStore.StorePublicKey = StorePublicKey;
                var result = await Client.Shop.UnlockSkuAsync(GameCode, SkuKind, SkuId);
                OnOwned?.Invoke(result.Owned);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
                OnOwned?.Invoke(false);
            }
        }
    }
}
