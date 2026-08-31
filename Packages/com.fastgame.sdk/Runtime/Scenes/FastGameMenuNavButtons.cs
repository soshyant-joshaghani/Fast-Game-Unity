using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>
    /// Inspector-visible navigation button slots for MAP_4_MENU.
    /// Expected GameObject names are documented per field — rename breaks validation warnings.
    /// </summary>
    [Serializable]
    public sealed class FastGameMenuNavButtons
    {
        [Header("Footer — main pages")]
        [Tooltip("Expected name: MENU_BTN · under Footer_Canvas")]
        public Button FooterMenu;

        [Tooltip("Expected name: SHOP_BTN · under Footer_Canvas")]
        public Button FooterShop;

        [Tooltip("Expected name: COLLECTIBLES_BTN · under Footer_Canvas")]
        public Button FooterCollectibles;

        [Header("Top header — main pages")]
        [Tooltip("Expected name: User_BTN · Header_Canvas (not inside a page canvas)")]
        public Button HeaderUser;

        [Tooltip("Expected name: Settings_BTN · Header_Canvas (not inside a page canvas)")]
        public Button HeaderSettings;

        [Header("Menu_Canvas sub-pages")]
        [Tooltip("Expected name: HOME_BTN")]
        public Button MenuHome;

        [Tooltip("Expected name: MAPS_BTN")]
        public Button MenuMaps;

        [Tooltip("Expected name: LOBBY_BTN")]
        public Button MenuLobby;

        [Header("Shop_Canvas sub-pages")]
        [Tooltip("Expected name: CHR_BTN")]
        public Button ShopCharacters;

        [Tooltip("Expected name: MAPS_BTN")]
        public Button ShopMaps;

        [Tooltip("Expected name: COLLECTIBLES_BTN")]
        public Button ShopCollectibles;

        [Header("Collectibles_Canvas sub-pages")]
        [Tooltip("Expected name: ACHIEVEMENTS_BTN")]
        public Button CollectiblesAchievements;

        [Tooltip("Expected name: AVATARS_BTN")]
        public Button CollectiblesAvatars;

        [Tooltip("Expected name: TITLES_BTN")]
        public Button CollectiblesTitles;

        [Header("User_Canvas")]
        [Tooltip("Expected name: Info_BTN")]
        public Button UserInfo;

        [Tooltip("Expected name: Friends_BTN")]
        public Button UserFriends;

        [Tooltip("Expected name: Notifs_BTN")]
        public Button UserNotifs;

        [Tooltip("Expected name: Chats_BTN")]
        public Button UserChats;

        [Tooltip("Expected name: SAVE_BTN")]
        public Button UserSave;

        [Tooltip("Expected name: Logout_BTN")]
        public Button UserLogout;

        [Header("Settings_Canvas sub-pages")]
        [Tooltip("Expected name: Settings_BTN")]
        public Button SettingsPage;

        [Tooltip("Expected name: ABOUT_BTN")]
        public Button SettingsAbout;

        struct Slot
        {
            public Button Button;
            public string ExpectedName;
            public FastGameMenuNavAction Action;
            public string Label;
        }

        public void PopulateMissingFromHierarchy(Transform root)
        {
            if (root == null)
                return;

            foreach (var slot in AllSlots())
            {
                if (slot.Button != null)
                    continue;

                var found = FastGameMenuNavWiring.FindByAction(root, slot.Action);
                if (found != null)
                    AssignSlot(slot.Action, found);
            }
        }

        public void Wire(FastGameMenuSceneBehaviour menu)
        {
            if (menu == null)
                return;

            foreach (var slot in AllSlots())
                FastGameMenuNavWiring.BindButton(slot.Button, slot.Action, menu);
        }

        public List<string> Validate()
        {
            var issues = new List<string>();
            foreach (var slot in AllSlots())
            {
                if (slot.Button == null)
                {
                    issues.Add($"Missing: {slot.Label} (expected `{slot.ExpectedName}`)");
                    continue;
                }

                if (!string.Equals(slot.Button.name, slot.ExpectedName, StringComparison.Ordinal))
                {
                    issues.Add(
                        $"{slot.Label}: GameObject is `{slot.Button.name}` but expected `{slot.ExpectedName}`");
                }

                var resolved = FastGameMenuNavWiring.Resolve(slot.Button.transform);
                if (resolved != slot.Action)
                {
                    issues.Add(
                        $"{slot.Label}: hierarchy context no longer maps to {slot.Action} (resolved {resolved})");
                }
            }
            return issues;
        }

        IEnumerable<Slot> AllSlots()
        {
            yield return SlotOf(FooterMenu, "MENU_BTN", FastGameMenuNavAction.MainMenu, "Footer → Menu");
            yield return SlotOf(FooterShop, "SHOP_BTN", FastGameMenuNavAction.MainShop, "Footer → Shop");
            yield return SlotOf(FooterCollectibles, "COLLECTIBLES_BTN", FastGameMenuNavAction.MainCollectibles, "Footer → Collectibles");
            yield return SlotOf(HeaderUser, "User_BTN", FastGameMenuNavAction.MainUser, "Header → User");
            yield return SlotOf(HeaderSettings, "Settings_BTN", FastGameMenuNavAction.MainSettings, "Header → Settings");
            yield return SlotOf(MenuHome, "HOME_BTN", FastGameMenuNavAction.MenuHome, "Menu → Home");
            yield return SlotOf(MenuMaps, "MAPS_BTN", FastGameMenuNavAction.MenuMaps, "Menu → Maps");
            yield return SlotOf(MenuLobby, "LOBBY_BTN", FastGameMenuNavAction.MenuLobby, "Menu → Lobby");
            yield return SlotOf(ShopCharacters, "CHR_BTN", FastGameMenuNavAction.ShopCharacters, "Shop → Characters");
            yield return SlotOf(ShopMaps, "MAPS_BTN", FastGameMenuNavAction.ShopMaps, "Shop → Maps");
            yield return SlotOf(ShopCollectibles, "COLLECTIBLES_BTN", FastGameMenuNavAction.ShopCollectibles, "Shop → Collectibles");
            yield return SlotOf(CollectiblesAchievements, "ACHIEVEMENTS_BTN", FastGameMenuNavAction.CollectiblesAchievements, "Collectibles → Achievements");
            yield return SlotOf(CollectiblesAvatars, "AVATARS_BTN", FastGameMenuNavAction.CollectiblesAvatars, "Collectibles → Avatars");
            yield return SlotOf(CollectiblesTitles, "TITLES_BTN", FastGameMenuNavAction.CollectiblesTitles, "Collectibles → Titles");
            yield return SlotOf(UserInfo, "Info_BTN", FastGameMenuNavAction.UserInfo, "User → Info");
            yield return SlotOf(UserFriends, "Friends_BTN", FastGameMenuNavAction.UserFriends, "User → Friends");
            yield return SlotOf(UserNotifs, "Notifs_BTN", FastGameMenuNavAction.UserNotifs, "User → Notifs");
            yield return SlotOf(UserChats, "Chats_BTN", FastGameMenuNavAction.UserChats, "User → Chats");
            yield return SlotOf(UserSave, "SAVE_BTN", FastGameMenuNavAction.UserSave, "User → Save");
            yield return SlotOf(UserLogout, "Logout_BTN", FastGameMenuNavAction.UserLogout, "User → Logout");
            yield return SlotOf(SettingsPage, "Settings_BTN", FastGameMenuNavAction.SettingsPage, "Settings → Settings");
            yield return SlotOf(SettingsAbout, "ABOUT_BTN", FastGameMenuNavAction.SettingsAbout, "Settings → About");
        }

        static Slot SlotOf(Button button, string expectedName, FastGameMenuNavAction action, string label) =>
            new Slot { Button = button, ExpectedName = expectedName, Action = action, Label = label };

        void AssignSlot(FastGameMenuNavAction action, Button button)
        {
            if (action == FastGameMenuNavAction.MainMenu) FooterMenu = button;
            else if (action == FastGameMenuNavAction.MainShop) FooterShop = button;
            else if (action == FastGameMenuNavAction.MainCollectibles) FooterCollectibles = button;
            else if (action == FastGameMenuNavAction.MainUser) HeaderUser = button;
            else if (action == FastGameMenuNavAction.MainSettings) HeaderSettings = button;
            else if (action == FastGameMenuNavAction.MenuHome) MenuHome = button;
            else if (action == FastGameMenuNavAction.MenuMaps) MenuMaps = button;
            else if (action == FastGameMenuNavAction.MenuLobby) MenuLobby = button;
            else if (action == FastGameMenuNavAction.ShopCharacters) ShopCharacters = button;
            else if (action == FastGameMenuNavAction.ShopMaps) ShopMaps = button;
            else if (action == FastGameMenuNavAction.ShopCollectibles) ShopCollectibles = button;
            else if (action == FastGameMenuNavAction.CollectiblesAchievements) CollectiblesAchievements = button;
            else if (action == FastGameMenuNavAction.CollectiblesAvatars) CollectiblesAvatars = button;
            else if (action == FastGameMenuNavAction.CollectiblesTitles) CollectiblesTitles = button;
            else if (action == FastGameMenuNavAction.UserInfo) UserInfo = button;
            else if (action == FastGameMenuNavAction.UserFriends) UserFriends = button;
            else if (action == FastGameMenuNavAction.UserNotifs) UserNotifs = button;
            else if (action == FastGameMenuNavAction.UserChats) UserChats = button;
            else if (action == FastGameMenuNavAction.UserSave) UserSave = button;
            else if (action == FastGameMenuNavAction.UserLogout) UserLogout = button;
            else if (action == FastGameMenuNavAction.SettingsPage) SettingsPage = button;
            else if (action == FastGameMenuNavAction.SettingsAbout) SettingsAbout = button;
        }
    }
}
