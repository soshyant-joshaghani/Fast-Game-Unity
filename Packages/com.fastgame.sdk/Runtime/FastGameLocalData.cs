using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// PlayerPrefs + download cache helpers. Used by Editor dev tools and runtime auth reset.
    /// </summary>
    public static class FastGameLocalData
    {
        public const string DownloadCacheFolder = "fastgame";

        public static void ClearAuthPrefs(FastGameConfig config = null)
        {
            config ??= new FastGameConfig();
            if (!string.IsNullOrEmpty(config.AccessTokenPrefsKey))
                PlayerPrefs.DeleteKey(config.AccessTokenPrefsKey);
            if (!string.IsNullOrEmpty(config.EnteredIdentityPrefsKey))
                PlayerPrefs.DeleteKey(config.EnteredIdentityPrefsKey);
            if (!string.IsNullOrEmpty(config.EnteredChannelPrefsKey))
                PlayerPrefs.DeleteKey(config.EnteredChannelPrefsKey);
            if (!string.IsNullOrEmpty(config.PendingPaymentPrefsKey))
                PlayerPrefs.DeleteKey(config.PendingPaymentPrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>Logout token, ENTER identity, and pending shop payment.</summary>
        public static void ClearAuthSession(FastGameConfig config = null)
        {
            config ??= new FastGameConfig();
            if (Application.isPlaying)
            {
                var auth = FastGameClientBehaviour.Instance?.Client?.Auth;
                if (auth != null)
                {
                    auth.ClearLocalCache();
                    return;
                }
            }
            ClearAuthPrefs(config);
        }

        public static void ClearEnteredIdentity(FastGameConfig config = null)
        {
            config ??= new FastGameConfig();
            if (Application.isPlaying)
            {
                var auth = FastGameClientBehaviour.Instance?.Client?.Auth;
                if (auth != null)
                {
                    auth.ClearEnteredIdentity();
                    return;
                }
            }
            if (!string.IsNullOrEmpty(config.EnteredIdentityPrefsKey))
                PlayerPrefs.DeleteKey(config.EnteredIdentityPrefsKey);
            if (!string.IsNullOrEmpty(config.EnteredChannelPrefsKey))
                PlayerPrefs.DeleteKey(config.EnteredChannelPrefsKey);
            PlayerPrefs.Save();
        }

        public static bool ClearDownloadCache()
        {
            var root = Path.Combine(Application.persistentDataPath, DownloadCacheFolder);
            if (!Directory.Exists(root))
                return false;
            Directory.Delete(root, true);
            return true;
        }

        public static void ClearAll(FastGameConfig config = null)
        {
            ClearAuthSession(config);
            FastGameLocalePrefs.Clear();
            ClearDownloadCache();
        }

        public static string Describe(FastGameConfig config = null)
        {
            config ??= new FastGameConfig();
            var sb = new StringBuilder();
            sb.AppendLine("PlayerPrefs");
            sb.AppendLine("  access token: " + Mask(PlayerPrefs.GetString(config.AccessTokenPrefsKey, "")));
            sb.AppendLine("  enter id: " + Mask(PlayerPrefs.GetString(config.EnteredIdentityPrefsKey, "")));
            sb.AppendLine("  enter channel: " + PlayerPrefs.GetString(config.EnteredChannelPrefsKey, "(none)"));
            sb.AppendLine("  pending payment: "
                + (PlayerPrefs.HasKey(config.PendingPaymentPrefsKey) ? "yes" : "no"));
            sb.AppendLine("  language: " + FastGameLocalePrefs.Get("(none)"));
            sb.AppendLine();
            sb.AppendLine("Download cache");
            var cacheRoot = Path.Combine(Application.persistentDataPath, DownloadCacheFolder);
            sb.AppendLine("  path: " + cacheRoot);
            sb.AppendLine("  exists: " + Directory.Exists(cacheRoot));
            if (Directory.Exists(cacheRoot))
                sb.AppendLine("  pack folders: " + CountSubdirectories(cacheRoot));
            if (Application.isPlaying && FastGameClientBehaviour.Instance?.Client?.Auth != null)
                sb.AppendLine("  play mode session: "
                    + (FastGameClientBehaviour.Instance.Client.Auth.IsAuthenticated ? "logged in" : "guest"));
            return sb.ToString();
        }

        static int CountSubdirectories(string root)
        {
            try
            {
                return Directory.GetDirectories(root, "*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
        }

        static string Mask(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "(none)";
            if (value.Length <= 4)
                return "****";
            return value.Substring(0, 2) + "…" + value.Substring(value.Length - 2);
        }
    }
}
