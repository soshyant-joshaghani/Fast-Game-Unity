# Unity FastGameStore (Android)

Official Myket / Cafe Bazaar / Google Play IAP for this Unity package. Fast Game remains the ownership source of truth (`POST …/shop/unlock/*` → `owned`).

Copy **exactly one** flavor into `src/com/fastgame/store/`:

| Flavor | File | Store app package | `StorePlatform` |
|--------|------|-------------------|-----------------|
| Myket | `flavors/myket/FastGameStoreActivity.java` | `ir.mservices.market` | `myket` |
| Cafe Bazaar | `flavors/cafebazaar/FastGameStoreActivity.kt` | `com.farsitel.bazaar` | `caffebazar` |
| Google Play | `flavors/googleplay/FastGameStoreActivity.java` | `com.android.vending` | `googleplay` |

Default checkout copy is **Myket**. Cafe Bazaar APK must ship the Kotlin Activity + Poolakey (delete the `.java` file).

**Initialize Game** `StorePlatform` must match. Designers call `Shop.UnlockSkuAsync` (FastGameStore is automatic). Missing store app → Initialize Game failure, not `owned`. Never fake owned. Never kill the process. `GetShopSkuAccessAsync` queries inventory (`openTheStorePage=false`) and restores Fast Game ownership if the store already owns the SKU. Cafe Bazaar / Myket RSA is set in the Editor and fetched after login — leave the Client `StorePublicKey` field empty unless you need a local override. Never put Pishkhan JWT / `api_secret` in the APK.

Ask players to keep **one Fast Game account**. Store wallet email need not match Fast Game phone — restore binds the receipt to the logged-in Fast Game user.

## Intent extras

- `openTheStorePage` (boolean) — purchase UI, or return existing inventory token
- `storeProductId` (string) — Fast Game `Payment.StoreProductId` from `store_skus.<provider>`
- `storePublicKey` (string, Myket / Cafe Bazaar) — store RSA public key (not Fast Game `api_secret`)

Callback: `FastGameStoreActivity.Listener` / `OnStorePurchase(storeProductId, purchaseToken, alreadyOwned)`. Empty token → fail.

## iOS

See [`../../iOS/README.md`](../../iOS/README.md) (StoreKit later; same Fast Game owned grant).
