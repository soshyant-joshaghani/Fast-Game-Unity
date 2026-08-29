# Scene shells — frozen NAMEs (UE + Unity)

Linear boot: **0 → 1 → 2 → 3 → 4 → LEVEL**

| # | Scene NAME | Main component |
|---|------------|----------------|
| 0 | `MAP_0_SPLASH` | `FastGameSplashBehaviour` |
| 1 | `MAP_1_LANGUAGE` | `FastGameLanguageSceneBehaviour` / `BP_1_LANGUAGE` |
| 2 | `MAP_2_AUTH` | `FastGameAuthBehaviour` / `BP_2_AUTH` |
| 3 | `MAP_3_DOWNLOAD` | `FastGameDownloadSceneBehaviour` / `BP_3_DOWNLOAD` |
| 4 | `MAP_4_MENU` | `FastGameMenuSceneBehaviour` / `BP_4_MENU` + `UFastGameMenuSceneComponent` |
| — | `MAP_LEVEL_SAMPLE` | `FastGameLevelSceneBehaviour` / `BP_5_LEVEL` |

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
- `ButtonContainer` + `LanguageButtonPrefab` (Button + Text/TMP)
- Builds one button per `supported_languages` from GetBootstrap (`en` fallback)
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

**Unity `FastGameMenuSceneBehaviour`**

| Setting | Default |
|---------|---------|
| **`Logout Scene`** | `MAP_1_LANGUAGE` |
| **`Logout()`** | wire menu Logout button → clears session + opens language scene |

Top-level: `MenuCanvas` | `ShopCanvas` | `CollectiblesCanvas`

Collectibles sub-pages: `AchievementsCanvas` | `TitlesCanvas` | `AvatarsCanvas`

Inspect detail: `InspectAchievementCanvas` | `InspectTitleCanvas` | `InspectAvatarCanvas`

Methods: `ShowMenu()` · `ShowShop()` · `ShowCollectibles()` · `ShowAchievements()` · `ShowTitles()` · `ShowAvatars()` · row click → `Inspect*()` · `CloseInspect()`

Optional list prefab: `CollectibleRowPrefab` + container per kind (loads from content API).

**UE `UFastGameMenuSceneComponent`** on `BP_4_MENU` — same `Show*` events; bind UMG visibility in widget graph.

## Backend splash pack

Publish an asset pack with `pack_id: splash` (or `kind: splash`) and `url` — returned in GetBootstrap as `splash_url`.
