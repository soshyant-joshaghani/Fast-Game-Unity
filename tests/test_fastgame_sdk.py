"""Fast Game SDK contract tests — Unity (no engine required)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "Packages/com.fastgame.sdk/Runtime"


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
    game_fn = client[
        client.find("public bool InitializeGame") : client.find(
            "public bool InitializeClient(string apiBaseUrl)"
        )
    ]
    net_fn = client[
        client.find("public bool InitializeClient(string apiBaseUrl)") : client.find(
            "public bool InitializeClient(string apiBaseUrl, string gameCode"
        )
    ]
    assert "EnsureSetup" in game_fn
    assert "EnsureSetup" not in net_fn
    assert "!InitializeGame()" in client
    assert "call Initialize Game" in _read(UNITY / "Shop/FastGameShop.cs")


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
        "Shop/FastGameShop.cs",
        "Ads/FastGameAds.cs",
        "Assets/FastGameAssets.cs",
        "Http/FastGameHttp.cs",
        "Store/FastGameStore.cs",
        "Store/FastGameStoreVerify.cs",
    ):
        path = UNITY / rel
        assert path.is_file(), rel
