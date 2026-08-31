using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// MAP_4_MENU — Menu / Shop / Collectibles / User / Settings with shared scroll views + INSPECT_CANVAS.
    /// Wire footer/header buttons to Show* methods; assign scroll views and sub-page canvases in Inspector.
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Menu")]
    public sealed class FastGameMenuSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Header("Main pages")]
        public GameObject MenuCanvas;
        public GameObject ShopCanvas;
        public GameObject CollectiblesCanvas;
        public GameObject UserCanvas;
        public GameObject SettingsCanvas;

        [Header("Menu sub-pages")]
        public GameObject MenuHomeCanvas;
        public Component MenuGameNameLabel;
        public FastGameItemsScrollView MenuMapsScroll;
        public FastGameItemsScrollView MenuLobbyScroll;

        [Header("Shop sub-pages (shared ITEMS_SCRLVIEW_H)")]
        public FastGameItemsScrollView ShopScroll;

        [Header("Collectibles sub-pages (shared ITEMS_SCRLVIEW_HV)")]
        public FastGameItemsScrollView CollectiblesScroll;

        [Header("User sub-pages")]
        public GameObject UserInfoCanvas;
        public GameObject UserFriendsCanvas;
        public GameObject UserNotifsCanvas;
        public GameObject UserChatsCanvas;
        public FastGameItemsScrollView UserListScroll;
        public Component UserPhoneLabel;
        public Component UserEmailLabel;
        public Component UserFullNameField;

        [Header("Settings sub-pages")]
        public GameObject SettingsPageCanvas;
        public GameObject AboutPageCanvas;

        [Header("Inspect overlay")]
        public FastGameInspectView InspectView;

        [Header("Shop / level")]
        public FastGameShopBehaviour Shop;

        [Header("Logout")]
        public string LogoutScene = FastGameSceneNames.Language;

        [Header("Navigation buttons")]
        [Tooltip("Inspector slots for every footer/header/sub-page button — check for missing or renamed *_BTN.")]
        public FastGameMenuNavButtons NavButtons = new FastGameMenuNavButtons();

        [Tooltip("Fill empty Nav Buttons slots from hierarchy before wiring.")]
        public bool PopulateMissingNavButtonsOnStart = true;

        [Tooltip("Log warnings when a slot is missing or the GameObject was renamed.")]
        public bool WarnOnNavValidationIssues = true;

        [Tooltip("Root for Find missing buttons — default scene Canvas.")]
        public Transform NavigationRoot;

        readonly Stack<MenuNavState> _navStack = new Stack<MenuNavState>();
        MenuNavState _currentNav;
        FastGameMenuItem _selectedItem;
        GameCatalogDetail _cachedGame;
        string _cachedGameLang;
        GameObject _panelHiddenForInspect;

        struct MenuNavState
        {
            public string MainPage;
            public string SubPage;
        }

        void Awake()
        {
            AutoLoadNextOnComplete = false;
            WireNavigationButtons();
        }

        void Start()
        {
            WireScrollHandlers();
            if (InspectView != null)
            {
                InspectView.OnBack.AddListener(CloseInspect);
                InspectView.OnAction.AddListener(OnInspectAction);
            }
            ShowMenuHome();
        }

        /// <summary>Wire Inspector nav slots. Optionally fill missing from hierarchy first.</summary>
        public void WireNavigationButtons()
        {
            if (PopulateMissingNavButtonsOnStart)
                PopulateNavigationButtons(missingOnly: true);

            NavButtons?.Wire(this);

            if (WarnOnNavValidationIssues && NavButtons != null)
            {
                foreach (var issue in NavButtons.Validate())
                    Debug.LogWarning("[FastGame Menu Nav] " + issue, this);
            }
        }

        /// <summary>Assign Nav Buttons from hierarchy (Editor button or runtime). Saves to scene when used in Editor.</summary>
        public void PopulateNavigationButtons(bool missingOnly = true)
        {
            var root = ResolveNavigationRoot();
            if (root == null || NavButtons == null)
                return;

            if (missingOnly)
                NavButtons.PopulateMissingFromHierarchy(root);
            else
            {
                NavButtons = new FastGameMenuNavButtons();
                NavButtons.PopulateMissingFromHierarchy(root);
            }
        }

        Transform ResolveNavigationRoot()
        {
            if (NavigationRoot != null)
                return NavigationRoot;
            var canvas = GetComponentInChildren<Canvas>(true);
            return canvas != null ? canvas.transform : transform;
        }

        /// <summary>Called by <see cref="FastGameMenuNavButton"/> — routes nav action to Show* methods.</summary>
        public void DispatchNav(FastGameMenuNavAction action)
        {
            switch (action)
            {
                case FastGameMenuNavAction.MainMenu: ShowMenu(); break;
                case FastGameMenuNavAction.MainShop: ShowShop(); break;
                case FastGameMenuNavAction.MainCollectibles: ShowCollectibles(); break;
                case FastGameMenuNavAction.MainUser: ShowUser(); break;
                case FastGameMenuNavAction.MainSettings: ShowSettings(); break;

                case FastGameMenuNavAction.MenuHome: ShowMenuHome(); break;
                case FastGameMenuNavAction.MenuMaps: ShowMenuMaps(); break;
                case FastGameMenuNavAction.MenuLobby: ShowMenuLobby(); break;

                case FastGameMenuNavAction.ShopCharacters: ShowShopCharacters(); break;
                case FastGameMenuNavAction.ShopMaps: ShowShopMaps(); break;
                case FastGameMenuNavAction.ShopCollectibles: ShowShopCollectibles(); break;

                case FastGameMenuNavAction.CollectiblesAchievements: ShowCollectiblesAchievements(); break;
                case FastGameMenuNavAction.CollectiblesAvatars: ShowCollectiblesAvatars(); break;
                case FastGameMenuNavAction.CollectiblesTitles: ShowCollectiblesTitles(); break;

                case FastGameMenuNavAction.UserInfo: ShowUserInfo(); break;
                case FastGameMenuNavAction.UserFriends: ShowUserFriends(); break;
                case FastGameMenuNavAction.UserNotifs: ShowUserNotifs(); break;
                case FastGameMenuNavAction.UserChats: ShowUserChats(); break;
                case FastGameMenuNavAction.UserSave: SaveUserFullName(); break;
                case FastGameMenuNavAction.UserLogout: Logout(); break;

                case FastGameMenuNavAction.SettingsPage: ShowSettingsPage(); break;
                case FastGameMenuNavAction.SettingsAbout: ShowAboutPage(); break;
            }
        }

        void WireScrollHandlers()
        {
            BindScroll(MenuMapsScroll, OpenInspect);
            BindScroll(MenuLobbyScroll, OpenInspect);
            BindScroll(ShopScroll, OpenInspect);
            BindScroll(CollectiblesScroll, OpenInspect);
            BindScroll(UserListScroll, OpenInspect);
        }

        static void BindScroll(FastGameItemsScrollView scroll, Action<FastGameMenuItem> handler)
        {
            if (scroll == null) return;
            scroll.OnItemClicked.RemoveAllListeners();
            scroll.OnItemClicked.AddListener(item => handler?.Invoke(item));
        }

        // --- Footer / header main pages ---

        public void ShowMenu() => ShowMenuHome();

        public void ShowShop()
        {
            ShowMain(ShopCanvas, "shop");
            ShowShopCharacters();
            if (Shop != null)
                Shop.RefreshCatalog();
        }

        public void ShowCollectibles()
        {
            ShowMain(CollectiblesCanvas, "collectibles");
            ShowCollectiblesAchievements();
        }

        public void ShowUser()
        {
            ShowMain(UserCanvas, "user");
            ShowUserInfo();
        }

        public void ShowSettings()
        {
            ShowMain(SettingsCanvas, "settings");
            ShowSettingsPage();
        }

        // --- Menu sub-pages ---

        public void ShowMenuHome()
        {
            ShowMain(MenuCanvas, "menu");
            ShowMenuSub("home", MenuHomeCanvas, MenuMapsScroll, MenuLobbyScroll);
            _ = RefreshGameNameAsync();
        }

        public void ShowMenuMaps()
        {
            ShowMain(MenuCanvas, "menu");
            ShowMenuSub("maps", MenuHomeCanvas, MenuMapsScroll, MenuLobbyScroll);
            _ = PopulateMenuMapsAsync();
        }

        public void ShowMenuLobby()
        {
            ShowMain(MenuCanvas, "menu");
            ShowMenuSub("lobby", MenuHomeCanvas, MenuMapsScroll, MenuLobbyScroll);
            _ = PopulateMenuLobbyAsync();
        }

        // --- Shop sub-pages ---

        public void ShowShopCharacters()
        {
            ShowMain(ShopCanvas, "shop");
            _currentNav = new MenuNavState { MainPage = "shop", SubPage = "chr" };
            _ = PopulateShopAsync("character");
        }

        public void ShowShopMaps()
        {
            ShowMain(ShopCanvas, "shop");
            _currentNav = new MenuNavState { MainPage = "shop", SubPage = "maps" };
            _ = PopulateShopAsync("map");
        }

        public void ShowShopCollectibles()
        {
            ShowMain(ShopCanvas, "shop");
            _currentNav = new MenuNavState { MainPage = "shop", SubPage = "collectibles" };
            _ = PopulateShopAsync(null, "achievement", "avatar", "title");
        }

        // --- Collectibles sub-pages ---

        public void ShowCollectiblesAchievements()
        {
            ShowMain(CollectiblesCanvas, "collectibles");
            _currentNav = new MenuNavState { MainPage = "collectibles", SubPage = "achievements" };
            _ = PopulateCollectiblesAsync(CollectiblesScroll, "achievement", LoadAchievementsAsync);
        }

        public void ShowCollectiblesAvatars()
        {
            ShowMain(CollectiblesCanvas, "collectibles");
            _currentNav = new MenuNavState { MainPage = "collectibles", SubPage = "avatars" };
            _ = PopulateCollectiblesAsync(CollectiblesScroll, "avatar", LoadAvatarsAsync);
        }

        public void ShowCollectiblesTitles()
        {
            ShowMain(CollectiblesCanvas, "collectibles");
            _currentNav = new MenuNavState { MainPage = "collectibles", SubPage = "titles" };
            _ = PopulateCollectiblesAsync(CollectiblesScroll, "title", LoadTitlesAsync);
        }

        /// <summary>Legacy alias — wire old Inspector buttons.</summary>
        public void ShowAchievements() => ShowCollectiblesAchievements();
        public void ShowAvatars() => ShowCollectiblesAvatars();
        public void ShowTitles() => ShowCollectiblesTitles();

        // --- User sub-pages ---

        public void ShowUserInfo()
        {
            ShowMain(UserCanvas, "user");
            ShowUserSub("info", UserInfoCanvas, UserFriendsCanvas, UserNotifsCanvas, UserChatsCanvas);
            SetActive(UserListScroll, false);
            _ = RefreshUserInfoAsync();
        }

        public void ShowUserFriends()
        {
            ShowMain(UserCanvas, "user");
            ShowUserSub("friends", UserFriendsCanvas, UserInfoCanvas, UserNotifsCanvas, UserChatsCanvas);
            SetActive(UserListScroll, true);
            _ = PopulateUserListAsync(LoadFriendsAsync);
        }

        public void ShowUserNotifs()
        {
            ShowMain(UserCanvas, "user");
            ShowUserSub("notifs", UserNotifsCanvas, UserInfoCanvas, UserFriendsCanvas, UserChatsCanvas);
            SetActive(UserListScroll, true);
            PopulatePlaceholderList(UserListScroll, "Notifications", "Coming soon");
        }

        public void ShowUserChats()
        {
            ShowMain(UserCanvas, "user");
            ShowUserSub("chats", UserChatsCanvas, UserInfoCanvas, UserFriendsCanvas, UserNotifsCanvas);
            SetActive(UserListScroll, true);
            PopulatePlaceholderList(UserListScroll, "Chats", "Coming soon");
        }

        public void SaveUserFullName()
        {
            var auth = FastGameClientBehaviour.Instance?.Client?.Auth;
            if (auth == null) return;
            var name = FastGameUiText.Read(UserFullNameField, "");
            _ = RunSaveNameAsync(auth, name);
        }

        public void Logout()
        {
            FastGameLocalData.ClearAuthSession();
            LoadScene(LogoutScene);
        }

        public void OpenLevel(string levelScene)
        {
            if (string.IsNullOrWhiteSpace(levelScene))
            {
                Debug.LogWarning(
                    "[FastGame Menu] Cannot open level: engine_scene is not configured for this map.",
                    this);
                return;
            }

            LoadScene(levelScene);
        }

        // --- Inspect ---

        public void OpenInspect(FastGameMenuItem item)
        {
            if (item == null)
                return;
            if (InspectView == null)
            {
                Debug.LogWarning("[FastGame Menu] InspectView is not assigned on Menu Scene Behaviour.", this);
                return;
            }

            _navStack.Push(_currentNav);
            _selectedItem = item;
            HidePanelForInspect();
            BuildInspectActions(item, out var a0, out var a1, out var a2);
            InspectView.Show(item, a0, a1, a2);
        }

        public void CloseInspect()
        {
            if (InspectView != null)
                InspectView.Hide();

            RestorePanelAfterInspect();

            if (_navStack.Count == 0)
                return;

            var nav = _navStack.Pop();
            RestoreNav(nav);
        }

        void OnInspectAction(FastGameMenuItem item, int actionIndex)
        {
            if (item == null) return;

            switch (item.Kind)
            {
                case "map":
                    if (actionIndex == 0)
                        OpenLevel(item.EngineScene);
                    break;
                case "shop":
                    if (actionIndex == 0 && Shop != null && !item.Owned)
                    {
                        Shop.SkuKind = item.SkuKind;
                        Shop.SkuId = item.SkuId;
                        Shop.UnlockSku();
                    }
                    break;
            }
        }

        void BuildInspectActions(
            FastGameMenuItem item,
            out FastGameInspectActionSlot a0,
            out FastGameInspectActionSlot a1,
            out FastGameInspectActionSlot a2)
        {
            a0 = Hidden();
            a1 = Hidden();
            a2 = Hidden();

            switch (item.Kind)
            {
                case "map":
                    if (!string.IsNullOrWhiteSpace(item.EngineScene))
                        a0 = Visible("Play solo");
                    a1 = Visible("Matchmake");
                    break;
                case "mode":
                    a0 = Visible("Find match");
                    break;
                case "shop":
                    a0 = item.Owned ? Hidden() : Visible("Purchase");
                    a1 = Visible("Details");
                    break;
                case "character":
                case "achievement":
                case "avatar":
                case "title":
                case "friend":
                    a0 = Visible("Details");
                    break;
                default:
                    a0 = Visible("Action 0");
                    a1 = Visible("Action 1");
                    a2 = Visible("Action 2");
                    break;
            }
        }

        static FastGameInspectActionSlot Visible(string label) =>
            new FastGameInspectActionSlot { Label = label, Visible = true };

        static FastGameInspectActionSlot Hidden() =>
            new FastGameInspectActionSlot { Visible = false };

        void RestoreNav(MenuNavState nav)
        {
            switch (nav.MainPage)
            {
                case "menu":
                    switch (nav.SubPage)
                    {
                        case "home": ShowMenuHome(); break;
                        case "maps": ShowMenuMaps(); break;
                        case "lobby": ShowMenuLobby(); break;
                        default: ShowMenuHome(); break;
                    }
                    break;
                case "shop":
                    switch (nav.SubPage)
                    {
                        case "chr": ShowShopCharacters(); break;
                        case "maps": ShowShopMaps(); break;
                        case "collectibles": ShowShopCollectibles(); break;
                        default: ShowShopCharacters(); break;
                    }
                    break;
                case "collectibles":
                    switch (nav.SubPage)
                    {
                        case "achievements": ShowCollectiblesAchievements(); break;
                        case "avatars": ShowCollectiblesAvatars(); break;
                        case "titles": ShowCollectiblesTitles(); break;
                        default: ShowCollectiblesAchievements(); break;
                    }
                    break;
                case "user":
                    switch (nav.SubPage)
                    {
                        case "info": ShowUserInfo(); break;
                        case "friends": ShowUserFriends(); break;
                        case "notifs": ShowUserNotifs(); break;
                        case "chats": ShowUserChats(); break;
                        default: ShowUserInfo(); break;
                    }
                    break;
                case "settings":
                    switch (nav.SubPage)
                    {
                        case "about": ShowAboutPage(); break;
                        default: ShowSettingsPage(); break;
                    }
                    break;
            }
        }

        // --- Settings sub-pages ---

        public void ShowSettingsPage()
        {
            ShowMain(SettingsCanvas, "settings");
            _currentNav = new MenuNavState { MainPage = "settings", SubPage = "settings" };
            SetActive(SettingsPageCanvas, true);
            SetActive(AboutPageCanvas, false);
        }

        public void ShowAboutPage()
        {
            ShowMain(SettingsCanvas, "settings");
            _currentNav = new MenuNavState { MainPage = "settings", SubPage = "about" };
            SetActive(SettingsPageCanvas, false);
            SetActive(AboutPageCanvas, true);
        }

        // --- Navigation helpers ---

        void ShowMain(GameObject active, string mainId)
        {
            CloseInspectSilent();
            FastGameUiPages.ShowOnly(active, MenuCanvas, ShopCanvas, CollectiblesCanvas, UserCanvas, SettingsCanvas);
            _currentNav = new MenuNavState { MainPage = mainId, SubPage = _currentNav.SubPage };
        }

        void ShowMenuSub(string subId, GameObject homeCanvas, FastGameItemsScrollView mapsScroll, FastGameItemsScrollView lobbyScroll)
        {
            _currentNav = new MenuNavState { MainPage = "menu", SubPage = subId };
            SetActive(homeCanvas, subId == "home");
            SetActive(mapsScroll, subId == "maps");
            SetActive(lobbyScroll, subId == "lobby");
        }

        void ShowUserSub(string subId, GameObject active, params GameObject[] others)
        {
            _currentNav = new MenuNavState { MainPage = "user", SubPage = subId };
            SetActive(active, true);
            foreach (var o in others)
                SetActive(o, false);
        }

        void CloseInspectSilent()
        {
            if (InspectView != null)
                InspectView.Hide();
            RestorePanelAfterInspect();
            _navStack.Clear();
        }

        void HidePanelForInspect()
        {
            RestorePanelAfterInspect();
            _panelHiddenForInspect = ResolveActiveMainCanvas();
            SetActive(_panelHiddenForInspect, false);
        }

        void RestorePanelAfterInspect()
        {
            if (_panelHiddenForInspect == null)
                return;
            SetActive(_panelHiddenForInspect, true);
            _panelHiddenForInspect = null;
        }

        GameObject ResolveActiveMainCanvas()
        {
            switch (_currentNav.MainPage)
            {
                case "shop": return ShopCanvas;
                case "collectibles": return CollectiblesCanvas;
                case "user": return UserCanvas;
                case "settings": return SettingsCanvas;
                default: return MenuCanvas;
            }
        }

        void HideInspect(bool hideView)
        {
            if (hideView && InspectView != null)
                InspectView.Hide();
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }

        static void SetActive(MonoBehaviour behaviour, bool active)
        {
            if (behaviour != null)
                behaviour.gameObject.SetActive(active);
        }

        // --- Data loading ---

        async Task RefreshGameNameAsync()
        {
            var lang = FastGameLocalePrefs.Get("en");
            var detail = await EnsureGameAsync();
            var host = FastGameClientBehaviour.Instance;
            var gameName = ResolveGameNameFallback(detail, host?.GameCode);
            var label = HasLocaleTranslations(detail?.Translations)
                ? FastGameDto.DisplayName(detail.Translations, gameName, lang)
                : gameName;
            if (string.IsNullOrWhiteSpace(label))
                label = gameName;
            FastGameUiText.WriteLabel(MenuGameNameLabel, label);
        }

        static bool HasLocaleTranslations(Dictionary<string, LocaleCopy> translations)
        {
            if (translations == null || translations.Count == 0)
                return false;
            foreach (var copy in translations.Values)
            {
                if (!string.IsNullOrWhiteSpace(copy?.Name))
                    return true;
            }
            return false;
        }

        static string ResolveGameNameFallback(GameCatalogDetail detail, string gameCode)
        {
            if (!string.IsNullOrWhiteSpace(detail?.Label))
                return detail.Label.Trim();
            if (!string.IsNullOrWhiteSpace(detail?.GameId))
                return detail.GameId.Trim();
            if (!string.IsNullOrWhiteSpace(gameCode))
                return gameCode.Trim();
            return "";
        }

        async Task PopulateMenuMapsAsync()
        {
            var detail = await EnsureGameAsync();
            var items = new List<FastGameMenuItem>();
            if (detail?.Maps != null)
            {
                foreach (var m in detail.Maps)
                {
                    var item = FastGameMenuItem.FromMap(m);
                    if (item != null)
                        items.Add(item);
                }
            }
            MenuMapsScroll?.SetItems(items);
        }

        async Task PopulateMenuLobbyAsync()
        {
            var detail = await EnsureGameAsync();
            var items = new List<FastGameMenuItem>();
            if (detail?.Modes != null)
            {
                foreach (var m in detail.Modes)
                {
                    var item = FastGameMenuItem.FromMode(m);
                    if (item != null) items.Add(item);
                }
            }
            (MenuLobbyScroll ?? MenuMapsScroll)?.SetItems(items);
        }

        async Task PopulateShopAsync(string singleKind = null, params string[] kinds)
        {
            if (ShopScroll == null)
                return;

            try
            {
                var host = FastGameClientBehaviour.Instance;
                if (host?.Client == null)
                {
                    Debug.LogWarning("[FastGame Menu] Shop: client not ready.", this);
                    ShopScroll.Clear();
                    return;
                }

                var lang = FastGameLocalePrefs.Get("en");
                List<ShopLine> catalog;
                if (Shop != null)
                {
                    await Shop.RefreshCatalogAsync();
                    catalog = Shop.LastCatalog ?? new List<ShopLine>();
                }
                else
                {
                    catalog = await host.Client.Shop.GetCatalogAsync(host.GameCode, lang);
                }

                var items = new List<FastGameMenuItem>();
                foreach (var line in catalog)
                {
                    if (singleKind != null && !string.Equals(line.SkuKind, singleKind, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (kinds != null && kinds.Length > 0)
                    {
                        var ok = false;
                        foreach (var k in kinds)
                        {
                            if (string.Equals(line.SkuKind, k, StringComparison.OrdinalIgnoreCase))
                            {
                                ok = true;
                                break;
                            }
                        }
                        if (!ok) continue;
                    }
                    var item = FastGameMenuItem.FromShopLine(line);
                    if (item != null) items.Add(item);
                }

                if (items.Count == 0)
                    Debug.Log($"[FastGame Menu] Shop tab returned 0 items (filter: {singleKind ?? string.Join(",", kinds ?? System.Array.Empty<string>())}).", this);

                ShopScroll.SetItems(items);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FastGame Menu] Shop load failed: " + e.Message, this);
                ShopScroll.Clear();
            }
        }

        async Task PopulateCollectiblesAsync(
            FastGameItemsScrollView scroll,
            string kind,
            Func<Task<List<CollectibleDef>>> loader)
        {
            if (scroll == null) return;
            try
            {
                var rows = await loader();
                var items = new List<FastGameMenuItem>();
                foreach (var row in rows)
                {
                    var item = FastGameMenuItem.FromCollectible(row, kind);
                    if (item != null) items.Add(item);
                }
                if (items.Count == 0)
                    Debug.Log($"[FastGame Menu] Collectibles/{kind}: API returned 0 rows.", this);
                scroll.SetItems(items);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FastGame Menu] Collectibles/{kind} load failed: {e.Message}", this);
                scroll.Clear();
            }
        }

        async Task PopulateUserListAsync(Func<Task<List<FastGameMenuItem>>> loader)
        {
            if (UserListScroll == null) return;
            try
            {
                UserListScroll.SetItems(await loader());
            }
            catch
            {
                UserListScroll.Clear();
            }
        }

        static void PopulatePlaceholderList(FastGameItemsScrollView scroll, string title, string subtitle)
        {
            scroll?.SetItems(new List<FastGameMenuItem>
            {
                new FastGameMenuItem
                {
                    Code = "placeholder",
                    Label = title,
                    Description = subtitle,
                    Kind = "placeholder",
                },
            });
        }

        async Task RefreshUserInfoAsync()
        {
            var auth = FastGameClientBehaviour.Instance?.Client?.Auth;
            if (auth == null) return;
            try
            {
                var me = await auth.GetMeAsync();
                FastGameUiText.WriteLabel(UserPhoneLabel, me?.Phone ?? "");
                FastGameUiText.WriteLabel(UserEmailLabel, me?.Email ?? "");
                FastGameUiText.Write(UserFullNameField, me?.FullName ?? "");
            }
            catch
            {
                // keep fields empty
            }
        }

        static async Task RunSaveNameAsync(FastGameAuth auth, string name)
        {
            try
            {
                await auth.UpdateFullNameAsync(name);
            }
            catch
            {
                // UI can show error later
            }
        }

        async Task<GameCatalogDetail> EnsureGameAsync()
        {
            var lang = FastGameLocalePrefs.Get("en");
            if (_cachedGame != null && string.Equals(_cachedGameLang, lang, StringComparison.OrdinalIgnoreCase))
                return _cachedGame;

            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
                return null;

            _cachedGame = await host.Client.Catalog.GetGameAsync(host.GameCode, lang, expandI18n: true);
            _cachedGameLang = lang;
            return _cachedGame;
        }

        async Task<List<CollectibleDef>> LoadAchievementsAsync() => await LoadCollectibles(
            (c, g, l) => c.ListAchievementsAsync(g, l));
        async Task<List<CollectibleDef>> LoadAvatarsAsync() => await LoadCollectibles(
            (c, g, l) => c.ListAvatarsAsync(g, l));
        async Task<List<CollectibleDef>> LoadTitlesAsync() => await LoadCollectibles(
            (c, g, l) => c.ListTitlesAsync(g, l));

        async Task<List<FastGameMenuItem>> LoadFriendsAsync()
        {
            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
                return new List<FastGameMenuItem>();
            var lang = FastGameLocalePrefs.Get("en");
            return await host.Client.Content.ListFriendsAsync(host.GameCode, lang);
        }

        static async Task<List<CollectibleDef>> LoadCollectibles(
            Func<FastGameContent, string, string, Task<List<CollectibleDef>>> fetch)
        {
            var host = FastGameClientBehaviour.Instance;
            if (host?.Client == null)
                return new List<CollectibleDef>();
            var lang = FastGameLocalePrefs.Get("en");
            return await fetch(host.Client.Content, host.GameCode, lang);
        }
    }
}
