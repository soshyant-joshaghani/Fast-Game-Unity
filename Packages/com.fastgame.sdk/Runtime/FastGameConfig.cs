using System;
using UnityEngine;

namespace FastGame
{
    [Serializable]
    public class FastGameConfig
    {
        /// <summary>
        /// Full API base URL (documented default <c>http://api.localhost/api/v1</c>).
        /// Host-only values like <c>api.localhost</c> still normalize to that form (same as Unreal).
        /// </summary>
        public string ApiBaseUrl = "http://api.localhost/api/v1";

        /// <summary>
        /// Active catalog game (storage NAME). Set once via Initialize Game.
        /// Auth OTP / recovery / signup verify send this as backend <c>game_code</c> —
        /// not as arguments on Enter / Login / Signup.
        /// </summary>
        public string GameCode = "";

        /// <summary>
        /// Target store / payment provider for this client build:
        /// myket | caffebazar | googleplay | steam | zarinpal | appstore.
        /// Empty shop Provider pins use this. Set via Initialize Game. Does not store auth identity (Enter does).
        /// </summary>
        public string StorePlatform = "";

        /// <summary>Myket / Cafe Bazaar RSA public key (not Fast Game api_secret). Fetched from Editor after login; optional override.</summary>
        public string StorePublicKey = "";

        public string PendingPaymentPrefsKey = "fast-game-client-shop-pending";

        /// <summary>PlayerPrefs key for persisted access token. Empty disables persistence.</summary>
        public string AccessTokenPrefsKey = "fast-game-client-access-token";

        /// <summary>PlayerPrefs key for ENTER-stored identity. Empty disables persistence.</summary>
        public string EnteredIdentityPrefsKey = "fast-game-client-entered-identity";

        /// <summary>PlayerPrefs key for ENTER-stored channel (email|phone).</summary>
        public string EnteredChannelPrefsKey = "fast-game-client-entered-channel";

        /// <summary>Dev / Production / EarlyAccess — must match <see cref="ClientAccessToken"/>.</summary>
        public FastGameProjectStage ProjectStage = FastGameProjectStage.Dev;

        /// <summary>Single build access token (dev shared per game; production issued to company owner).</summary>
        public string ClientAccessToken = "";

        /// <summary>Assigned by POST /apps/games/client/initialize; used for heartbeat.</summary>
        public string ClientInstanceId = "";

        /// <summary>
        /// Accept host-only values like <c>api.localhost</c> and normalize to
        /// <c>http://api.localhost/api/v1</c>. Matches Unreal <c>NormalizeApiBaseUrl</c>.
        /// </summary>
        public static string NormalizeApiBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "http://api.localhost/api/v1";

            url = url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            while (url.EndsWith("/", StringComparison.Ordinal) && url.Length > 1)
                url = url.Substring(0, url.Length - 1);

            if (url.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
                return url;

            // Host only (no path) → append /api/v1. Custom prefixes are left as-is.
            var schemeSep = url.IndexOf("://", StringComparison.Ordinal);
            if (schemeSep >= 0)
            {
                var rest = url.Substring(schemeSep + 3);
                if (rest.IndexOf('/') < 0)
                {
                    url += "/api/v1";
                }
                else
                {
                    Debug.LogWarning(
                        "FastGame: ApiBaseUrl '" + url
                        + "' has a path but does not end with /api/v1. "
                        + "Login expects …/api/v1/base/login/access-token");
                }
            }

            return url;
        }

        public static string NormalizeProviderId(string provider)
        {
            var id = (provider ?? "").Trim().ToLowerInvariant();
            if (id == "cafebazaar" || id == "cafe_bazaar" || id == "bazaar" || id == "cafe-bazaar")
                return "caffebazar";
            if (id == "google_play" || id == "play" || id == "google-play")
                return "googleplay";
            if (id == "app_store" || id == "ios" || id == "apple")
                return "appstore";
            return id;
        }

        public static string StorePlatformToId(FastGameStorePlatform platform)
        {
            switch (platform)
            {
                case FastGameStorePlatform.Myket:
                    return "myket";
                case FastGameStorePlatform.CafeBazaar:
                    return "caffebazar";
                case FastGameStorePlatform.GooglePlay:
                    return "googleplay";
                case FastGameStorePlatform.Steam:
                    return "steam";
                case FastGameStorePlatform.ZarinPal:
                    return "zarinpal";
                case FastGameStorePlatform.AppStore:
                    return "appstore";
                default:
                    return "";
            }
        }

        public static FastGameStorePlatform StorePlatformFromId(string provider)
        {
            var id = NormalizeProviderId(provider);
            if (id == "myket") return FastGameStorePlatform.Myket;
            if (id == "caffebazar") return FastGameStorePlatform.CafeBazaar;
            if (id == "googleplay") return FastGameStorePlatform.GooglePlay;
            if (id == "steam") return FastGameStorePlatform.Steam;
            if (id == "zarinpal") return FastGameStorePlatform.ZarinPal;
            if (id == "appstore") return FastGameStorePlatform.AppStore;
            return FastGameStorePlatform.Unset;
        }

        public static string StoreDisplayName(string provider)
        {
            var id = NormalizeProviderId(provider);
            if (id == "caffebazar") return "Cafe Bazaar";
            if (id == "myket") return "Myket";
            if (id == "googleplay") return "Google Play";
            return string.IsNullOrEmpty(id) ? "the store app" : id;
        }

        public static string StoreNotInstalledMessage(string provider)
        {
            var name = StoreDisplayName(provider);
            return name + " is not installed on this device. Install " + name + ", then open the game again.";
        }
    }

    public enum FastGameShopProgress
    {
        Success,
        Pending,
        Failed,
        Cancelled,
        StoreMissing
    }
}
