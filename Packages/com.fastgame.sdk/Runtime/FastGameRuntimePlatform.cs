using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Runtime OS + quality class for DOWNLOAD pack filtering (cross-platform §2).
    /// </summary>
    public static class FastGameRuntimePlatform
    {
        public static string GetRuntimeOs()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "mac";
                case RuntimePlatform.WebGLPlayer:
                    return "web";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "windows";
                default:
                    return "windows";
            }
        }

        public static string GetQualityClass(string runtimeOs = null)
        {
            runtimeOs ??= GetRuntimeOs();
            return runtimeOs is "windows" or "mac" ? "pc" : "mobile";
        }

        /// <summary>Store flavor hint for editor smoke (pack platforms[] still use OS ids).</summary>
        public static string StorePlatformToOs(string storePlatform)
        {
            var id = FastGameConfig.NormalizeProviderId(storePlatform ?? "");
            if (id is "myket" or "caffebazar" or "googleplay")
                return "android";
            if (id == "steam")
                return "windows";
            if (id == "appstore")
                return "ios";
            if (id == "macstore")
                return "mac";
            return "";
        }
    }
}
