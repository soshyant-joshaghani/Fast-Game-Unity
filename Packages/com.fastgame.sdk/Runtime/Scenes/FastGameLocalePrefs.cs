using UnityEngine;

namespace FastGame
{
    /// <summary>Guest / client preferred language until synced after AUTH (B1a).</summary>
    public static class FastGameLocalePrefs
    {
        const string Key = "fastgame.preferred_language";

        public static string Get(string fallback = "en") =>
            PlayerPrefs.GetString(Key, string.IsNullOrWhiteSpace(fallback) ? "en" : fallback);

        public static void Set(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return;
            PlayerPrefs.SetString(Key, language.Trim().ToLowerInvariant());
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
