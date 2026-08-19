"""FastGameStore SDK tests — Unity Android flavors and OS surfaces."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PKG = ROOT / "Packages/com.fastgame.sdk"
UNITY_STORE = PKG / "Plugins/Android/FastGameStore"
UNITY_FLAVORS = UNITY_STORE / "flavors"
UNITY_RUNTIME_STORE = PKG / "Runtime/Store/FastGameStore.cs"

FLAVORS = {
    "myket": ("myket/FastGameStoreActivity.java", "ir.mservices.market", "isMyketInstalled"),
    "cafebazaar": ("cafebazaar/FastGameStoreActivity.kt", "com.farsitel.bazaar", "isMarketAppInstalled"),
    "googleplay": ("googleplay/FastGameStoreActivity.java", "com.android.vending", "isPlayStoreInstalled"),
}


def _read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def test_android_flavors_present():
    assert UNITY_FLAVORS.is_dir(), f"android flavor root missing: {UNITY_FLAVORS}"
    for rel, _pkg, _fn in FLAVORS.values():
        src = UNITY_FLAVORS / rel
        assert src.is_file(), f"missing {rel}"


def test_android_install_checks_and_no_kill():
    myket = _read(UNITY_FLAVORS / "myket/FastGameStoreActivity.java")
    bazaar = _read(UNITY_FLAVORS / "cafebazaar/FastGameStoreActivity.kt")
    play = _read(UNITY_FLAVORS / "googleplay/FastGameStoreActivity.java")
    assert "ir.mservices.market" in myket and "isMyketInstalled" in myket
    assert "com.farsitel.bazaar" in bazaar and "isMarketAppInstalled" in bazaar
    assert "com.android.vending" in play and "isPlayStoreInstalled" in play
    for src in (myket, bazaar, play):
        assert "OnStorePurchase" in src
        assert "openTheStorePage" in src
        assert "storeProductId" in src
        assert "killProcess" not in src
        assert "exitProcess" not in src
    assert "native void OnStorePurchase" in myket
    assert "native void OnStorePurchase" in play
    assert "external fun OnStorePurchase" in bazaar
    assert "purchaseToken" in bazaar
    assert "storePublicKey" in myket and "storePublicKey" in bazaar
    assert "Loading ..." in bazaar
    assert "normalizeRsaPublicKey" in bazaar
    assert "getSubscribedProducts" in bazaar
    assert "retry without local RSA" in bazaar
    assert "re-query inventory after Bazaar purchase UI" in bazaar
    assert "disconnected {" in bazaar and "finish()" not in bazaar.split("disconnected {")[1].split("}")[0]


def test_unity_store_internal():
    runtime = _read(UNITY_RUNTIME_STORE)
    assert "RequestPurchaseTokenAsync" in runtime
    assert "IsStoreAppInstalled" in runtime
    behaviour = _read(PKG / "Runtime/Components/FastGameStoreBehaviour.cs")
    assert 'AddComponentMenu("Fast Game/Store")' not in behaviour
    assert "Obsolete" in behaviour
