# Scene shells — frozen NAMEs (UE + Unity)

Linear boot: **0 → 1 → 2 → 3 → 4** — then per-map level scenes from catalog **`engine_scene`**.

| # | Scene NAME | Main component |
|---|------------|----------------|
| 0 | `MAP_0_SPLASH` | `FastGameSplashBehaviour` |
| 1 | `MAP_1_LANGUAGE` | `FastGameLanguageSceneBehaviour` / `BP_1_LANGUAGE` |
| 2 | `MAP_2_AUTH` | `FastGameAuthBehaviour` / `BP_2_AUTH` |
| 3 | `MAP_3_DOWNLOAD` | `FastGameDownloadSceneBehaviour` / `BP_3_DOWNLOAD` |
| 4 | `MAP_4_MENU` | `FastGameMenuSceneBehaviour` / `BP_4_MENU` + `UFastGameMenuSceneComponent` |
| level | per map | `FastGameLevelSceneBehaviour` / `BP_5_LEVEL` — scene NAME from dashboard **`engine_scene`** |

## SPLASH (0)

Expected hierarchy:

```text
SPLASH_CANVAS             ← FastGameSplashBehaviour
  SPLASH_BG               ← FastGameSplashBackgroundView (+ stretch Image, solid color)
  SPLASH_IMAGE            ← FastGameSplashImageView (+ Image or RawImage)
  SPLASH_VIDEO (optional) ← FastGameSplashVideoView (+ VideoPlayer when module enabled)
```

**`Fast Game/Splash`** on canvas:
- **`Next Scene`** — default `MAP_1_LANGUAGE`
- **`Splash Background`** — SPLASH_BG color layer (always visible during splash)
- **`Splash Image` / `Splash Video`** — drag SPLASH_IMAGE / SPLASH_VIDEO (auto-found if empty)
- **`Fetch Online`** — image from fast-game tip (not shipped in build); video is **not** used online
- **`Fetch Online` off** — `LocalSprite` on Image View, or local VideoPlayer clip on Video View
- **`Local Priority`** — Prefer Video / Prefer Image / Image only / Video only
- **`Image Display Seconds`** — timer for image (local or online); **video advances on playback end**
- **`Video Fallback Seconds`** — safety timeout if video never finishes
- **Auto-skip** — no local and no online splash → immediate next scene

Image/Video view components do **not** hide themselves on play — only the main Splash behaviour controls visibility.

## LANGUAGE (1)

**Unity `FastGameLanguageSceneBehaviour`**
- **`Language Scroll`** — assign `ITEMS_SCRLVIEW_H` (uses `ITEMS_THUMB` + horizontal layout)
- Legacy: `ButtonContainer` + `LanguageButtonPrefab` if scroll not assigned
- Builds one button per `supported_languages` from **GetBootstrap** (any locale codes the game publishes)
- **`Fallback Languages`** — default `en`, `fa`, `ar` when bootstrap / tip is unavailable
- Unknown locale codes use **English** culture names (`Persian`, `Arabic`, `German`, …) — avoids font gaps with native scripts
- **`Advance On Select`** (default on) — tap language → save locale → next scene
- **`Skip Auth When Authenticated`** — logged-in users go to **`MAP_3_DOWNLOAD`** instead of AUTH
- **`Auto Advance When Authenticated`** — skip language UI entirely when session exists
- **`Continue()`** — when Advance On Select is off (wire a Continue button)

**UE `BP_1_LANGUAGE`** — list widget from bootstrap languages; Continue → Scene Flow

## AUTH (2)

**Unity `FastGameAuthBehaviour`** — single component on `AUTH` (or `Auth_Canvas`):

| Setting | Default |
|---------|---------|
| **`Next Scene`** | `MAP_3_DOWNLOAD` |
| **`Auto Load Next On Complete`** | on |
| **`Complete When Already Authenticated`** | skip UI if session exists |

| Canvas | Back → Enter ID | Other buttons |
|--------|-----------------|---------------|
| EnterID | — | Enter |
| EnterPassword | `BackFromPasswordButton` | Login, **ForgotPasswordButton** |
| Signup | — | Create account |
| OTP | `BackFromOtpButton` | **SendCodeButton** (resend), Verify |
| NewPassword | — | Reset |

- `BackToEnterId()` — from Enter Password / OTP only (keeps identity)
- `BeginForgotPassword()` — Enter Password → OTP recovery
- `AutoSendOtpOnShow` — auto **Send Auth Code** when OTP page opens (signup verify or forgot)
- Auth success → loads **`Next Scene`** automatically

## DOWNLOAD (3)

**Unity `FastGameDownloadSceneBehaviour`** on `DOWNLOAD` root:

| Setting | Default |
|---------|---------|
| **`Next Scene`** | `MAP_4_MENU` |
| **`Auto Start`** | fetch + download on enter |
| **`Advance When Nothing To Download`** | skip to menu if no packs / tip unpublished |
| **`Skip Splash Packs`** | don't re-download splash assets |

Optional: **`Progress Slider`**, **`Status Label`** (Text or TMP).

### Published tip vs draft packs

| | Draft asset-packs | Published tip |
|--|-------------------|---------------|
| API | `GET /asset-packs/{game}/packs` (panel) | `GET /tip/{game}/game` (player) |
| When | Editor CRUD + uploads | After **Publish tip** on game config |
| Unity DOWNLOAD | **Not used** | **Required** — 404 until published |

**Unblock:** panel → upload pack parts (ready) → set `quality`, `platforms`, `languages`, `kind` → **Publish tip**.

**Filter** (from LANGUAGE locale + runtime OS + quality class):

- `quality` includes `mobile`|`pc` or `*`
- `platforms` includes `android`|`ios`|`windows`|`mac`|`web` or `*`
- `languages` includes preferred language from `FastGameLocalePrefs` or `*`

Store flavor (`myket`, `googleplay`, …) selects **which APK you built**; pack `platforms[]` still use **OS ids**. Author fa-only packs for Bazaar/Myket, multi-lang for Google Play, etc.

**Dev tools:** **Fast Game → Dev Tools…** → **Check tip published** (calls GetBootstrap).

**UE** — bind widget buttons:
- **Back To Enter ID** (`BackToEnterId`) on password + OTP widgets
- **Begin Forgot Password** on password widget
- OTP widget **OnShown** → **Notify OTP Page Shown** (auto-send once; **Send Auth Code** for resend)
- **On Auth Complete** → Scene Flow → DOWNLOAD

## MENU (4)

**Unity `FastGameMenuSceneBehaviour`** on menu root (e.g. `MenuCanvas` parent):

### Main pages (footer / header)

| Method | Page |
|--------|------|
| `ShowMenu()` / `ShowMenuHome()` | Menu → Home |
| `ShowShop()` | Shop |
| `ShowCollectibles()` | Collectibles |
| `ShowUser()` | User |
| `ShowSettings()` | Settings |

### Sub-pages (per-page header buttons)

| Main | Methods |
|------|---------|
| Menu | `ShowMenuHome()`, `ShowMenuMaps()`, `ShowMenuLobby()` |
| Shop | `ShowShopCharacters()`, `ShowShopMaps()`, `ShowShopCollectibles()` |
| Collectibles | `ShowCollectiblesAchievements()`, `ShowCollectiblesAvatars()`, `ShowCollectiblesTitles()` |
| User | `ShowUserInfo()`, `ShowUserFriends()`, `ShowUserNotifs()`, `ShowUserChats()` |
| Settings | `ShowSettingsPage()`, `ShowAboutPage()` |

### Prefab components

| Prefab | Component | Notes |
|--------|-----------|-------|
| `ITEMS_THUMB` | `FastGameItemsThumbView` | `THUMB_IMG`, `THUMB_TXT`, click → inspect |
| `ITEMS_SCRLVIEW_H` / `_V` / `_HV` | `FastGameItemsScrollView` | Set **Layout** = Horizontal / Vertical / Grid |
| `INSPECT_CANVAS` | `FastGameInspectView` | Name, description, image, 3 actions, **Back** |

Assign shared scroll views on the menu behaviour (e.g. one `ShopScroll` for CHR/MAPS/COLLECTIBLES tabs). **Inspect** uses a single overlay — **Back** restores the previous main + sub page via an internal nav stack.

| Setting | Default |
|---------|---------|
| **`Logout Scene`** | `MAP_1_LANGUAGE` |
| **`Logout()`** | wire User Info logout button |
| **Map play** | Inspect → **Play solo** loads the map's catalog **`engine_scene`** (must be in Build Settings) |

User Info: assign `UserPhoneLabel`, `UserEmailLabel`, `UserFullNameField`; wire **Save** → `SaveUserFullName()`.

### Navigation buttons (Inspector slots)

Every nav button has an explicit slot on **`FastGameMenuSceneBehaviour` → Nav Buttons** (expected `*_BTN` name in each tooltip). This makes renames and missing wiring visible in the Inspector instead of failing silently.

| Inspector section | Button names |
|-------------------|--------------|
| **Footer** | `MENU_BTN`, `SHOP_BTN`, `COLLECTIBLES_BTN` |
| **Top Header** | `User_BTN`, `Settings_BTN` |
| **Menu_Canvas** | `HOME_BTN`, `MAPS_BTN`, `LOBBY_BTN` |
| **Shop_Canvas** | `CHR_BTN`, `MAPS_BTN`, `COLLECTIBLES_BTN` |
| **Collectibles_Canvas** | `ACHIEVEMENTS_BTN`, `AVATARS_BTN`, `TITLES_BTN` |
| **User_Canvas** | `Info_BTN`, `Friends_BTN`, `Notifs_BTN`, `Chats_BTN`, `SAVE_BTN`, `Logout_BTN` |
| **Settings_Canvas** | `Settings_BTN`, `ABOUT_BTN` |

Duplicate names (`MAPS_BTN`, `COLLECTIBLES_BTN`, `Settings_BTN`) resolve by **which page canvas** the button lives under.

**Inspector diagnostics** (custom editor on the menu behaviour):

- Yellow warning if a slot is empty, the GameObject was renamed, or hierarchy context no longer matches.
- **Find missing buttons** — fill empty slots from hierarchy.
- **Find all buttons (overwrite)** — re-scan and replace every slot.
- **Wire navigation now** — bind OnClick without entering Play mode.

Runtime: **`Populate Missing Nav Buttons On Start`** fills empty slots; **`Warn On Nav Validation Issues`** logs mismatches to the Console.

Optional: add **`FastGameMenuNavButton`** on a button and set **Action** explicitly to override auto-detect.

**UE** — bind widget buttons:

## Backend splash pack

Publish an asset pack with `pack_id: splash` (or `kind: splash`) and `url` — returned in GetBootstrap as `splash_url`.
