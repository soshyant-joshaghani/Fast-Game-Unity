namespace FastGame
{
    /// <summary>Target for menu footer/header/sub-page buttons.</summary>
    public enum FastGameMenuNavAction
    {
        None = 0,

        // Main pages (footer / top header)
        MainMenu,
        MainShop,
        MainCollectibles,
        MainUser,
        MainSettings,

        // Menu sub-pages
        MenuHome,
        MenuMaps,
        MenuLobby,

        // Shop sub-pages
        ShopCharacters,
        ShopMaps,
        ShopCollectibles,

        // Collectibles sub-pages
        CollectiblesAchievements,
        CollectiblesAvatars,
        CollectiblesTitles,

        // User sub-pages + actions
        UserInfo,
        UserFriends,
        UserNotifs,
        UserChats,
        UserSave,
        UserLogout,

        // Settings sub-pages
        SettingsPage,
        SettingsAbout,
    }
}
