# Fast Game SDK — Unity

UPM package `com.fastgame.sdk` — **FastAPI client only**.

## Install

1. Package `com.fastgame.sdk` is **pre-installed** via `Packages/manifest.json`.
   In another project: Package Manager → Add package from disk → select `Packages/com.fastgame.sdk`.
2. Config: `ApiBaseUrl = http://api.localhost/api/v1` (documented full base; host-only `api.localhost` still normalizes the same way as Unreal).
3. Set **GameCode** on **Fast Game → Client** (active catalog title). Auth OTP / recovery use it — not Enter / Login / Signup args.
4. Android IAP: copy **exactly one** flavor from `Plugins/Android/FastGameStore/flavors/` into `src/com/fastgame/store/` — see [Plugins/Android/FastGameStore/README.md](Plugins/Android/FastGameStore/README.md).
5. iOS StoreKit later: [Plugins/iOS/README.md](Plugins/iOS/README.md).

## Editor dev tools

**Fast Game → Dev Tools…** — inspect saved token / Enter ID / language / download cache; clear individually or all.

**Published tip:** use **Check tip published** (GetBootstrap). Draft asset-packs in the panel are **not** what Unity DOWNLOAD reads — players need **Publish tip** on game config (`POST …/tip/{game}/admin/publish`). Until published, `GetGameConfig` returns 404 and DOWNLOAD shows *Tip not published*.

Quick menu (no window):

- **Fast Game → Clear All Local Data**
- **Fast Game → Clear Auth Session** — logout token + Enter ID + pending payment
- **Fast Game → Clear Download Cache** — `persistentDataPath/fastgame/packs`

Works in and out of Play mode. After clearing auth while playing, re-enter from Enter ID or restart Play.

## Scene components (UE-like)

Mirror of Unreal **Fast Game** subsystem nodes — drop components on GameObjects and wire UI Buttons / UnityEvents.

| Component | Menu | Role |
|-----------|------|------|
| `FastGameClientBehaviour` | **Fast Game → Client** | **Initialize Game** (GameCode + StorePlatform + OS) then **Initialize Client** (ApiBaseUrl / reconnect); DontDestroyOnLoad |
| `FastGameAuthBehaviour` | **Fast Game → Auth** | Enter / Enter Password / Verify / Signup / Send Auth Code / Assign New Password |
| `FastGameShopBehaviour` | **Fast Game → Shop** | Catalog / claim / **Unlock Sku** / Complete Unlock / Shop Progress (empty GameId → client) |

### Setup

1. Empty GameObject → **Add Component → Fast Game → Client** → **Initialize Game** fields: `GameCode` + `StorePlatform` (`myket` / `caffebazar` / `googleplay`); **Initialize Client** field: `ApiBaseUrl` = `http://api.localhost/api/v1`. Awake runs Initialize Game then Initialize Client (1-arg network). Do **not** use the obsolete 3-arg `InitializeClient(url, gameCode, store)`. Fast Game phone login need not match Myket/Cafe Bazaar email.
2. Auth GameObject → **Fast Game → Auth** (optional: drag Identity / Password InputFields or TMP fields)
3. Shop GameObject → **Fast Game → Shop** → set `GameId`

### Auth (ENTER contract + canvases)

Hierarchy:

```text
AUTH (global canvas)                 ← AuthCanvas (optional)
  Enter ID Canvas                    ← EnterIdCanvas
  Enter Password Canvas              ← EnterPasswordCanvas
  Signup Canvas                      ← EnterSignupCanvas  (name + password + confirm)
  OTP Canvas                         ← EnterRecoveryOtpCanvas  (Verify + recovery)
  Assign New Password Canvas         ← EnterRecoveryResetCanvas  (recovery confirm)
```

Assign those under **Pages** on Auth. With **Auto Switch Pages** on, `Enter()` shows the right page (no SetActive wiring needed).

| Button | OnClick method | Canvas |
|--------|----------------|--------|
| Enter / Continue | `Enter` | Enter ID |
| Enter Password / Login | `Login` | Enter Password |
| Signup / Register | `Signup` / `Register` | Signup (Register or Complete Account via LastEnterRoute) |
| Send Auth Code | `SendCode` | OTP (Verify → signup OTP; Enter Password → recovery OTP) |
| Verify Auth Code | `VerifyCode` | OTP → Signup \| Assign New Password |
| Assign New Password | `AssignNewPassword` / `ResetPassword` | recovery confirm (password + confirm) |
| Update name | `UpdateFullName` | after login (`PATCH /me`) |
| Back | `Back` | clears identity + fields + error → Enter ID |

Assign a shared **Error Text** (TMP Text under AUTH) — failures write there and clear on the next action / Back.

```text
Enter() → auto page
  Enter Password   → Login()  (+ Send Auth Code → OTP → Assign New Password)
  Signup (seeded)  → Signup() / Register()  (name + password; /complete)
  Verify           → SendCode() → VerifyCode() → Signup
  Signup (new)     → Signup() / Register()
Back() → reset id → Enter ID
```

Inspector: assign **per-panel** TMP_InputField roots — **Login Password**, **Signup Password** + **Signup Confirm** + name, **Recovery OTP**, **Recovery Password** + **Recovery Confirm**. Child Text is OK (parent input is resolved). Or set the string fallbacks for debugging. Name is shown for Signup and hidden for Assign New Password.

### Shop (after login)

```text
Auth.Login succeeded → Shop.RefreshCatalog() → OnCatalogLoaded
Claim free: set SkuKind/SkuId → Shop.ClaimFree()
Unlock (all platforms): Shop.UnlockSku() or Shop.UnlockSkuAsync(game, kind, id, callbackUrl)
  Empty GameId / Provider on Shop → Initialize Game (same as Enter empty identity)
  Store IAP completes native token automatically on device
  ZarinPal/Steam: after return/overlay → Shop.CompleteUnlock()
Access: Shop.GetSkuAccess() → OnSkuAccess(locked, owned)  // optional UI Branch; Android store also queries inventory + restore
Login freezes the current Myket/Cafe Bazaar/Play wallet so mid-game store-account switches cannot restore another wallet onto this Fast Game user.

Studio graphs (gate `full_game`, Myket ↔ Cafe Bazaar same Fast Game login): [fast-game/docs/sdk-shop-flows.md](../../../fast-game/docs/sdk-shop-flows.md).
```

## C# API usage

```csharp
var client = new FastGameClient(new FastGameConfig
{
    ApiBaseUrl = "http://api.localhost/api/v1",
    GameCode = "sandbox-capsule", // active title — auth OTP / recovery use this
});
// host-only "api.localhost" also normalizes to …/api/v1

// ENTER contract — probe then route in your UI (no SDK widgets)
// On success, stores enter.Identity for later calls with empty identity
var enter = await client.Auth.EnterAsync(identity);
// designer pins: Enter Password / Verify / Signup / Failed
// internal: exists && !PasswordRequired → Login (Enter Password)
//           exists && PasswordRequired  → Signup pin (CompleteAccount; Register → /complete)
//           !exists + verify ON         → Verify (signup OTP) → Register
//           !exists + verify OFF        → Signup
var (verifyPhone, verifyEmail) = await client.Catalog.GetAuthRequirementsAsync(client.Config.GameCode);

// With stored identity (after Enter):
await client.Auth.LoginAsync("", password);
await client.Auth.CompleteAccountAsync(password, passwordConfirm, fullName);
await client.Auth.RequestSignupVerificationAsync("");   // Verify → signup OTP
await client.Auth.VerifySignupVerificationAsync("", code);
await client.Auth.SignupAsync(null, password, passwordConfirm, fullName, null);  // Register
// From Enter Password: Send Auth Code (recovery) → Verify Auth Code → Assign New Password
await client.Auth.RequestPasswordRecoveryAsync("");
await client.Auth.VerifyPasswordRecoveryAsync("", code);
await client.Auth.ConfirmPasswordRecoveryAsync("", newPassword, newPasswordConfirm);
await client.Auth.UpdateFullNameAsync(fullName);
// Or explicit:
await client.Auth.LoginAsync(enter.Identity, password,
    enter.IsEmail ? FastGameIdentityChannel.Email : FastGameIdentityChannel.Phone);

// Register with explicit contacts:
await client.Auth.SignupAsync(email, password, passwordConfirm, fullName, phone);

var me = await client.Auth.GetMeAsync(); // Id, Email, Phone, FullName, …
var session = await client.Content.PrepareSessionAsync("sandbox-capsule", "sandbox", "box-arena", lang: "fa");
var shop = await client.Shop.GetCatalogAsync("sandbox-capsule", lang: "fa");
// expandI18n: true only when you need the full translations map (default: resolved Label only)

// Clear ENTER-stored identity when switching accounts:
client.Auth.ClearEnteredIdentity();
// Dev tool on login page (token + ENTER store + pending payment):
client.Auth.ClearLocalCache();
```

`FastGameIdentity.Classify(identity)` returns `Email` / `Phone` / `Unknown`. Password confirmation is checked in the SDK; only one password value is sent to the backend.

The access token is stored in PlayerPrefs (`fast-game-client-access-token`) and restored when the client is constructed. ENTER identity uses `fast-game-client-entered-identity` + `fast-game-client-entered-channel`. `Logout` clears the token; `ClearEnteredIdentity` / `ClearLocalCache` clear the ENTER store.

## Multiplayer (Realtime.JoinMap + sibling Colyseus)

1. Install the official [Colyseus Unity SDK](https://docs.colyseus.io/getting-started/unity-sdk/) as a **separate** package.
2. Prefer tip Get* for player content; mint a seat then join Colyseus with the token:

```csharp
var bootstrap = await client.Content.GetBootstrapAsync(gameCode);
var gameCfg = await client.Content.GetGameConfigAsync(gameCode);
var mapCfg = await client.Content.GetMapConfigAsync(gameCode, mapId);

var seat = await client.Realtime.JoinMapAsync(gameCode, mapId, modeId);
// Colyseus sibling — not FastGame:
// var coly = new Colyseus.ColyseusClient(seat.GameServerUrl);
// var room = await coly.JoinOrCreate<object>(seat.RoomName, new {
//     seat_token = seat.SeatToken,
//     gameId = seat.GameId, mapId = seat.MapId, modeId = seat.ModeId,
// }, seat.SeatToken);
```

Do **not** teach raw Colyseus join with designer-chosen `gameId`/`mapId`. Prefer seat over `Catalog.GetGameServerAsync`.

## Modules

See [../../../CONTRACT.md](../../../CONTRACT.md).

### Ads

`client.Ads.GetAdvertisementAsync` / `TrackEventAsync`. `Advertisement.Media.Url` and text-ad `meta.background_url` are **absolute CDN URLs** from game storage (platform storage and/or Arvan / Liara / custom). Load them with `UnityWebRequest` / `VideoPlayer` as-is — do not rewrite against `ApiBaseUrl`.

## Samples

- `Samples~/ApiOnly` — catalog / spawn / shop (Fast Game only)
- `Samples~/SandboxMultiplayer` — PrepareSession + how to join with sibling Colyseus
