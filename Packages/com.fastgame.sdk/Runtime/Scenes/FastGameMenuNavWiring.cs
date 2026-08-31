using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>Maps *_BTN names + page canvas context → <see cref="FastGameMenuNavAction"/>.</summary>
    public static class FastGameMenuNavWiring
    {
        public static Button FindByAction(Transform root, FastGameMenuNavAction action)
        {
            if (root == null || action == FastGameMenuNavAction.None)
                return null;

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button != null && Resolve(button.transform) == action)
                    return button;
            }
            return null;
        }

        public static void BindButton(Button button, FastGameMenuNavAction action, FastGameMenuSceneBehaviour menu)
        {
            if (button == null || menu == null || action == FastGameMenuNavAction.None)
                return;

            var nav = button.GetComponent<FastGameMenuNavButton>();
            if (nav == null)
                nav = button.gameObject.AddComponent<FastGameMenuNavButton>();
            nav.Action = action;
            nav.Menu = menu;
            nav.Bind(menu);
        }

        /// <summary>Legacy scan — prefer explicit <see cref="FastGameMenuNavButtons"/> in Inspector.</summary>
        public static void WireUnder(Transform root, FastGameMenuSceneBehaviour menu)
        {
            if (root == null || menu == null)
                return;

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button == null)
                    continue;

                var action = Resolve(button.transform);
                if (action != FastGameMenuNavAction.None)
                    BindButton(button, action, menu);
            }
        }

        public static FastGameMenuNavAction Resolve(Transform t)
        {
            if (t == null)
                return FastGameMenuNavAction.None;

            var name = t.name;
            var page = FindPageCanvas(t);

            if (page == "Menu_Canvas")
                return ResolveMenuSub(name);
            if (page == "Shop_Canvas")
                return ResolveShopSub(name);
            if (page == "Collectibles_Canvas")
                return ResolveCollectiblesSub(name);
            if (page == "User_Canvas")
                return ResolveUserSub(name);
            if (page == "Settings_Canvas")
                return ResolveSettingsSub(name);

            if (IsUnderNamed(t, "Footer_Canvas") || IsUnderNamed(t, "Footer_Panel"))
                return ResolveFooterMain(name);

            if (IsUnderNamed(t, "Header_Canvas") && page == null)
                return ResolveGlobalHeader(name);

            return FastGameMenuNavAction.None;
        }

        static FastGameMenuNavAction ResolveFooterMain(string name) => name switch
        {
            "MENU_BTN" => FastGameMenuNavAction.MainMenu,
            "SHOP_BTN" => FastGameMenuNavAction.MainShop,
            "COLLECTIBLES_BTN" => FastGameMenuNavAction.MainCollectibles,
            _ => FastGameMenuNavAction.None,
        };

        static FastGameMenuNavAction ResolveGlobalHeader(string name) => name switch
        {
            "User_BTN" => FastGameMenuNavAction.MainUser,
            "Settings_BTN" => FastGameMenuNavAction.MainSettings,
            _ => FastGameMenuNavAction.None,
        };

        static FastGameMenuNavAction ResolveMenuSub(string name) => name switch
        {
            "HOME_BTN" => FastGameMenuNavAction.MenuHome,
            "MAPS_BTN" => FastGameMenuNavAction.MenuMaps,
            "LOBBY_BTN" => FastGameMenuNavAction.MenuLobby,
            _ => FastGameMenuNavAction.None,
        };

        static FastGameMenuNavAction ResolveShopSub(string name) => name switch
        {
            "CHR_BTN" => FastGameMenuNavAction.ShopCharacters,
            "MAPS_BTN" => FastGameMenuNavAction.ShopMaps,
            "COLLECTIBLES_BTN" => FastGameMenuNavAction.ShopCollectibles,
            _ => FastGameMenuNavAction.None,
        };

        static FastGameMenuNavAction ResolveCollectiblesSub(string name) => name switch
        {
            "ACHIEVEMENTS_BTN" => FastGameMenuNavAction.CollectiblesAchievements,
            "AVATARS_BTN" => FastGameMenuNavAction.CollectiblesAvatars,
            "TITLES_BTN" => FastGameMenuNavAction.CollectiblesTitles,
            _ => FastGameMenuNavAction.None,
        };

        static FastGameMenuNavAction ResolveUserSub(string name) => name switch
        {
            "Info_BTN" => FastGameMenuNavAction.UserInfo,
            "Friends_BTN" => FastGameMenuNavAction.UserFriends,
            "Notifs_BTN" => FastGameMenuNavAction.UserNotifs,
            "Chats_BTN" => FastGameMenuNavAction.UserChats,
            "SAVE_BTN" => FastGameMenuNavAction.UserSave,
            "Logout_BTN" => FastGameMenuNavAction.UserLogout,
            _ => FastGameMenuNavAction.None,
        };

        static FastGameMenuNavAction ResolveSettingsSub(string name) => name switch
        {
            "Settings_BTN" => FastGameMenuNavAction.SettingsPage,
            "ABOUT_BTN" => FastGameMenuNavAction.SettingsAbout,
            _ => FastGameMenuNavAction.None,
        };

        public static string FindPageCanvas(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                switch (p.name)
                {
                    case "Menu_Canvas":
                    case "Shop_Canvas":
                    case "Collectibles_Canvas":
                    case "User_Canvas":
                    case "Settings_Canvas":
                        return p.name;
                }
            }
            return null;
        }

        static bool IsUnderNamed(Transform t, string ancestorName)
        {
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (p.name == ancestorName)
                    return true;
            }
            return false;
        }
    }
}
