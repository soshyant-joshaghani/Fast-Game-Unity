"""Fast Game SDK contract tests — Unity (no engine required)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "Packages/com.fastgame.sdk/Runtime"
SAMPLES = ROOT / "Packages/com.fastgame.sdk/Samples~"


def _read(rel: Path) -> str:
    return rel.read_text(encoding="utf-8")


def test_unity_unlock_and_ensure_setup():
    shop = _read(UNITY / "Shop/FastGameShop.cs")
    client = _read(UNITY / "Components/FastGameClientBehaviour.cs")
    assert "/apps/games/shop/unlock/begin" in shop
    assert "/apps/games/shop/unlock/complete" in shop
    assert "/apps/games/shop/unlock/restore" in shop
    assert "UnlockSkuAsync" in shop
    assert "CompleteUnlockAsync" in shop
    assert "bool InitializeGame" in client
    assert "bool InitializeClient" in client
    assert "EnsureSetup" in client
    assert "StorePublicKey" in client
    assert "EnsureStoreVerifyKeyAsync" in shop
    assert "/apps/games/catalog/" in shop
    assert "store-verify-key" in shop
    assert (UNITY / "Store/FastGameStoreVerify.cs").is_file()
    assert "FG1" in _read(UNITY / "Store/FastGameStoreVerify.cs")
    assert "ir.mservices.market" in _read(UNITY / "Store/FastGameStore.cs")
    assert "com.farsitel.bazaar" in _read(UNITY / "Store/FastGameStore.cs")
    assert "com.android.vending" in _read(UNITY / "Store/FastGameStore.cs")


def test_unity_initialize_game_vs_client():
    client = _read(UNITY / "Components/FastGameClientBehaviour.cs")
    assert "public bool InitializeClient(string apiBaseUrl)" in client
    assert "public bool InitializeGame" in client
    three_arg = "public bool InitializeClient(string apiBaseUrl, string gameCode, string storePlatform)"
    assert three_arg in client
    # 3-arg kept for compat but must be Obsolete
    idx = client.find(three_arg)
    prelude = client[max(0, idx - 280) : idx]
    assert "[Obsolete" in prelude or "System.Obsolete" in prelude
    game_fn = client[
        client.find("public bool InitializeGame") : client.find(
            "public bool InitializeClient(string apiBaseUrl)"
        )
    ]
    net_fn = client[
        client.find("public bool InitializeClient(string apiBaseUrl)") : client.find(three_arg)
    ]
    assert "EnsureSetup" in game_fn
    assert "EnsureSetup" not in net_fn
    assert "!InitializeGame()" in client
    assert "call Initialize Game" in _read(UNITY / "Shop/FastGameShop.cs")


def test_unity_samples_no_buy_or_three_arg_init():
    api_only = _read(SAMPLES / "ApiOnly/FastGameApiOnlySample.cs")
    assert "BuyAsync" not in api_only
    assert "VerifyPendingAsync" not in api_only
    assert "UnlockSkuAsync" in api_only
    assert "CompleteUnlockAsync" in api_only
    assert "InitializeClient(" not in api_only or "InitializeClient(string apiBaseUrl, string gameCode" not in api_only
    assert "http://api.localhost/api/v1" in api_only


def test_unity_store_lock_on_login():
    shop = _read(UNITY / "Shop/FastGameShop.cs")
    client = _read(UNITY / "FastGameClient.cs")
    auth = _read(UNITY / "Auth/FastGameAuth.cs")
    behaviour = _read(UNITY / "Components/FastGameClientBehaviour.cs")
    assert "/apps/games/shop/store-lock" in shop
    assert "BindStoreLockAsync" in shop
    assert "OnLoggedIn" in auth
    assert "BindStoreLockAsync" in client
    assert "BindStoreLockAsync" in behaviour


def test_unity_shop_access_queries_store():
    shop = _read(UNITY / "Shop/FastGameShop.cs")
    store = _read(UNITY / "Store/FastGameStore.cs")
    assert "store_product_id" in shop
    assert "TrySyncStoreOwnershipAsync" in shop
    assert "PurchaseOrRestoreAsync(trimmed, false)" in shop
    assert "/apps/games/shop/unlock/restore" in shop
    assert "openStorePage = true" in store


def test_unity_shop_empty_pins_use_initialize_game():
    shop = _read(UNITY / "Shop/FastGameShop.cs")
    behaviour = _read(UNITY / "Components/FastGameShopBehaviour.cs")
    assert 'code = (_config.GameCode ?? "").Trim()' in shop
    assert "NormalizeProviderId(_config.StorePlatform)" in shop
    assert "gameId = ResolveGameCode(gameId)" in shop
    assert "ResolveProvider(null)" in shop
    assert "ResolvedGameId" in behaviour
    assert "Empty → Initialize Game" in behaviour


def test_unity_enter_not_wiped_on_reinit():
    client = _read(UNITY / "Components/FastGameClientBehaviour.cs")
    auth = _read(UNITY / "Auth/FastGameAuth.cs")
    assert "ClearEnteredIdentity" not in client
    assert "Logout" not in client
    assert "EnteredIdentity" in auth
    assert "/base/login/enter" in auth or "EnterAsync" in auth


def test_unity_modules_present():
    for rel in (
        "FastGameClient.cs",
        "FastGameConfig.cs",
        "Auth/FastGameAuth.cs",
        "Catalog/FastGameCatalog.cs",
        "Content/FastGameContent.cs",
        "Realtime/FastGameRealtime.cs",
        "Shop/FastGameShop.cs",
        "Ads/FastGameAds.cs",
        "Assets/FastGameAssets.cs",
        "Http/FastGameHttp.cs",
        "Store/FastGameStore.cs",
        "Store/FastGameStoreVerify.cs",
    ):
        path = UNITY / rel
        assert path.is_file(), rel


def test_unity_tip_facade_content_methods():
    content = _read(UNITY / "Content/FastGameContent.cs")
    contract = _read(ROOT / "CONTRACT.md")
    assert "GetBootstrapAsync" in content
    assert "GetGameConfigAsync" in content
    assert "GetMapConfigAsync" in content
    assert "GetCharacterAsync" in content
    assert "GetDialogueAsync" in content
    assert "GetQuizAsync" in content
    assert "GetStringsAsync" in content
    assert "/apps/games/tip/" in content
    assert "/apps/games/strings/" in content
    assert "/bootstrap" in content
    assert '"/game"' in content or "/game\"" in content or '}/game"' in content
    assert "GetBootstrap" in contract
    assert "GetGameConfig" in contract
    assert "GetMapConfig" in contract
    assert "GetPackTipAsync" in content
    assert "/apps/games/asset-packs/" in content
    assert "payload" in _read(UNITY / "Assets/FastGameAssets.cs")
    assert "deprecated for players" in contract.lower()


def test_unity_download_pack_filter_and_platform():
    selector = _read(UNITY / "FastGamePackSelector.cs")
    platform = _read(UNITY / "FastGameRuntimePlatform.cs")
    download = _read(UNITY / "Components/FastGameDownloadSceneBehaviour.cs")
    dto = _read(UNITY / "Models/FastGameDto.cs")
    models = _read(UNITY / "Models/Models.cs")

    assert "FastGamePackSelector" in selector
    assert "MatchesTagList" in selector
    assert 'tag == "*"' in selector or 'tag == \"*\"' in selector
    assert "SkipSplashPacks" in selector
    assert "GetRuntimeOs" in platform
    assert "GetQualityClass" in platform
    assert "StorePlatformToOs" in platform
    assert "myket" in platform
    assert "steam" in platform

    assert "GetGameConfigAsync" in download
    assert "FastGamePackSelector.ListForDownload" in download
    assert "FastGamePackDownload" in download
    assert "Tip not published" in download
    assert "ResolveDownloadUrlAsync" in download

    assert 'ParseStringList(p, "quality")' in dto
    assert 'ParseStringList(p, "platforms")' in dto
    assert 'ParseStringList(p, "languages")' in dto
    assert "public List<string> Quality" in models
    assert "public List<string> Platforms" in models
    assert "public List<string> Languages" in models


def test_unity_language_scene_scroll():
    lang = _read(UNITY / "Components/FastGameLanguageSceneBehaviour.cs")
    scroll = _read(UNITY / "Components/FastGameItemsScrollView.cs")

    assert "FastGameItemsScrollView LanguageScroll" in lang
    assert "ParseSupportedLanguages" in lang
    assert '"fa"' in lang
    assert '"ar"' in lang
    assert "TryCultureEnglishName" in lang
    assert "LanguageScroll.SetItems" in lang
    assert "public IReadOnlyList<FastGameItemsThumbView> Rows" in scroll


def test_unity_menu_ui_prefab_components():
    menu = _read(UNITY / "Components/FastGameMenuSceneBehaviour.cs")
    thumb = _read(UNITY / "Components/FastGameItemsThumbView.cs")
    scroll = _read(UNITY / "Components/FastGameItemsScrollView.cs")
    inspect = _read(UNITY / "Components/FastGameInspectView.cs")
    item = _read(UNITY / "Models/FastGameMenuItem.cs")

    assert "ShowUser()" in menu
    assert "ShowSettings()" in menu
    assert "ShowMenuMaps()" in menu
    assert "ShowShopCharacters()" in menu
    assert "FastGameInspectView" in menu
    assert "CloseInspect" in menu
    assert "_navStack" in menu

    assert "FastGameItemsThumbView" in thumb
    assert "THUMB_TXT" in thumb
    assert "FastGameScrollLayout" in scroll
    assert "SetItems" in scroll
    assert "FastGameInspectView" in inspect
    assert "BACK_BTN" in inspect
    assert "FastGameMenuItem" in item
    assert "FromShopLine" in item


def test_unity_menu_nav_buttons():
    menu = _read(UNITY / "Components/FastGameMenuSceneBehaviour.cs")
    nav_buttons = _read(UNITY / "Scenes/FastGameMenuNavButtons.cs")
    nav_btn = _read(UNITY / "Components/FastGameMenuNavButton.cs")
    wiring = _read(UNITY / "Scenes/FastGameMenuNavWiring.cs")
    action = _read(UNITY / "Scenes/FastGameMenuNavAction.cs")
    editor = _read(UNITY.parent / "Editor/FastGameMenuSceneBehaviourEditor.cs")

    assert "NavButtons" in menu
    assert "PopulateMissingNavButtonsOnStart" in menu
    assert "WireNavigationButtons" in menu
    assert "DispatchNav" in menu
    assert "Validate()" in nav_buttons
    assert "FooterMenu" in nav_buttons
    assert "Expected name: MENU_BTN" in nav_buttons
    assert "FastGameMenuNavButton" in nav_btn
    assert "FastGameMenuNavWiring" in wiring
    assert "FindByAction" in wiring
    assert "BindButton" in wiring
    assert "MENU_BTN" in wiring
    assert "HOME_BTN" in wiring
    assert "CHR_BTN" in wiring
    assert "ACHIEVEMENTS_BTN" in wiring
    assert "MainShop" in action
    assert "ShopCollectibles" in action
    assert "Navigation diagnostics" in editor


def test_unity_progress_get_save():
    progress = _read(UNITY / "Progress/FastGameProgress.cs")
    client = _read(UNITY / "FastGameClient.cs")
    contract = _read(ROOT / "CONTRACT.md")
    assert "GetAsync" in progress
    assert "SaveAsync" in progress
    assert "/apps/games/progress/" in progress
    assert "FastGameProgress Progress" in client
    assert "client.Progress" in contract
    assert "/apps/games/progress/" in contract


def test_unity_realtime_joinmap_seat():
    realtime = _read(UNITY / "Realtime/FastGameRealtime.cs")
    client = _read(UNITY / "FastGameClient.cs")
    contract = _read(ROOT / "CONTRACT.md")
    assert "MintSeatAsync" in realtime
    assert "JoinMapAsync" in realtime
    assert "/apps/games/realtime/seat" in realtime
    assert "FastGameRealtime Realtime" in client
    assert "Realtime.JoinMap" in contract
    assert "/apps/games/realtime/seat" in contract
    assert "designer-chosen" in contract.lower() or "designer-chosen" in contract


def test_unity_map_hub_and_engine_scene():
    models = _read(UNITY / "Models/Models.cs")
    dto = _read(UNITY / "Models/FastGameDto.cs")
    item = _read(UNITY / "Models/FastGameMenuItem.cs")
    menu = _read(UNITY / "Components/FastGameMenuSceneBehaviour.cs")
    scenes = _read(SAMPLES / "Scenes/SCENE_NAMES.md")

    assert "MapRuntimeSettings" in models
    assert "EngineScene" in models
    assert "MapKind" in models
    assert "HubMapIds" in models
    assert "AbilityAllowlist" in models
    assert "ParseMapKind" in dto
    assert "ParseMapRuntimeSettings" in dto
    assert "engine_scene" in dto
    assert "hub_map_ids" in dto
    assert "runtime_settings" in dto
    assert "EngineScene" in item
    assert "MapKind" in item
    assert "HubMapIds" in item
    assert "OpenLevel(item.EngineScene)" in menu
    assert "DefaultLevelScene" not in menu
    assert "LevelSample" not in _read(UNITY / "Scenes/FastGameSceneNames.cs")
    assert "engine_scene" in scenes
    assert "Default Level Scene" not in scenes
