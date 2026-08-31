namespace FastGame
{
    /// <summary>
    /// Frozen scene / map NAMEs — must match UE <c>MAP_*</c> assets and Unity <c>Assets/Scenes</c>.
    /// </summary>
    public static class FastGameSceneNames
    {
        public const string Splash = "MAP_0_SPLASH";
        public const string Language = "MAP_1_LANGUAGE";
        public const string Auth = "MAP_2_AUTH";
        public const string Download = "MAP_3_DOWNLOAD";
        public const string Menu = "MAP_4_MENU";

        /// <summary>Default linear boot order (0 → 4).</summary>
        public static readonly string[] BootOrder =
        {
            Splash,
            Language,
            Auth,
            Download,
            Menu
        };

        public static string NextInBoot(string current)
        {
            for (var i = 0; i < BootOrder.Length - 1; i++)
            {
                if (BootOrder[i] == current)
                    return BootOrder[i + 1];
            }
            return null;
        }
    }
}
