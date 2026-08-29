using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame;
using FastGame.Models;
using UnityEngine;

namespace FastGame.Samples
{
    /// <summary>
    /// API-only demo: login, PrepareSession (map/chr/stats), shop list / claim / Unlock Sku / Complete Unlock.
    /// No Colyseus required. Does not teach obsolete Buy* / VerifyPending.
    /// </summary>
    public sealed class FastGameApiOnlySample : MonoBehaviour
    {
        public string ApiBaseUrl = "http://api.localhost/api/v1";
        public string Identity = "admin@example.com";
        public string Password = "changethis";
        public string GameId = "sandbox-capsule";
        public string ModeId = "sandbox";
        public string MapId = "box-arena";
        public string PaymentCallbackUrl = "http://dashboard.localhost/panel/playground";

        FastGameClient _client;
        PreparedSession _session;
        List<ShopLine> _shop = new List<ShopLine>();
        string _status = "idle";
        Vector2 _scroll;

        void Awake()
        {
            _client = new FastGameClient(new FastGameConfig
            {
                ApiBaseUrl = ApiBaseUrl,
                GameCode = GameId,
            });
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 480, Screen.height - 24));
            GUILayout.Label("Fast Game — ApiOnly sample");
            GUILayout.Label("Status: " + _status);

            if (GUILayout.Button("1. Login"))
                _ = Login();

            if (GUILayout.Button("2. PrepareSession (catalog + runtime + spawn)"))
                _ = Prepare();

            if (GUILayout.Button("3. Refresh shop"))
                _ = RefreshShop();

            if (_client.Shop.HasPendingPayment && GUILayout.Button("Complete Unlock (pending)"))
                _ = CompleteUnlock();

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(320));
            foreach (var line in _shop)
            {
                GUILayout.BeginHorizontal();
                var meta = "";
                if (line.Meta != null && line.Meta.ContainsKey("off_percent"))
                    meta = $" -{line.Meta["off_percent"]}%";
                GUILayout.Label($"{line.SkuKind}:{line.SkuId}  {line.Label}  {line.Price} Rial{meta}" +
                               (line.Owned ? " [owned]" : ""), GUILayout.Width(340));
                if (!line.Owned)
                {
                    if (line.Price <= 0)
                    {
                        if (GUILayout.Button("Claim"))
                            _ = Claim(line);
                    }
                    else if (GUILayout.Button("Unlock Sku"))
                        _ = Unlock(line);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        async Task Login()
        {
            try
            {
                _status = "logging in…";
                await _client.Auth.LoginAsync(Identity, Password);
                _status = "logged in";
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task Prepare()
        {
            try
            {
                _status = "preparing…";
                _session = await _client.Content.PrepareSessionAsync(GameId, ModeId, MapId);
                var chr = FastGameJson.GetObject(_session.Spawn, "character");
                var label = FastGameJson.GetString(chr, "label");
                var stats = FastGameJson.GetObject(chr, "stats");
                _status = $"session ok — character={label} stats={FastGameJson.Stringify(stats)} room={_session.ColyseusRoom}";
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task RefreshShop()
        {
            try
            {
                _shop = await _client.Shop.GetCatalogAsync(GameId);
                _status = $"shop lines: {_shop.Count}";
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task CompleteUnlock()
        {
            try
            {
                var res = await _client.Shop.CompleteUnlockAsync();
                _status = res.Success ? "purchase ok" : "payment failed";
                _shop = await _client.Shop.GetCatalogAsync(GameId);
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task Claim(ShopLine line)
        {
            try
            {
                await _client.Shop.ClaimFreeAsync(line.GameCode, line.SkuKind, line.SkuId);
                _status = "claimed " + line.SkuId;
                _shop = await _client.Shop.GetCatalogAsync(GameId);
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task Unlock(ShopLine line)
        {
            try
            {
                _status = "Unlock Sku…";
                var result = await _client.Shop.UnlockSkuAsync(
                    line.GameCode, line.SkuKind, line.SkuId, PaymentCallbackUrl);
                if (result.Owned)
                    _status = "owned " + line.SkuId;
                else if (result.Pending)
                    _status = "pending — return and Complete Unlock";
                else
                    _status = "unlock: " + (result.Mode ?? "unknown");
                _shop = await _client.Shop.GetCatalogAsync(GameId);
            }
            catch (System.Exception e) { _status = e.Message; }
        }
    }
}
