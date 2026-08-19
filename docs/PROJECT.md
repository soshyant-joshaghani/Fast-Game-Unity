# Fast Game Unity — project guide

Guide for **this** Unity project: what exists today, how it relates to Fast Game, and production rules to follow when game content lands here.

For package API usage see [Packages/com.fastgame.sdk/README.md](../Packages/com.fastgame.sdk/README.md). For HTTP contracts see [CONTRACT.md](../CONTRACT.md).

---

## What this repo is today

| Status | Item |
|--------|------|
| **Exists** | Unity **6.3 LTS** project (`ProjectSettings/ProjectVersion.txt`) |
| **Exists** | UPM [`com.fastgame.sdk`](../Packages/com.fastgame.sdk) — FastAPI client |
| **Exists** | Android IAP under `Packages/com.fastgame.sdk/Plugins/Android/FastGameStore` |
| **Exists** | Samples (import via Package Manager → **Fast Game SDK → Samples**) |
| **Exists** | [`tests/`](../tests/) + [`run_tests.py`](../run_tests.py) — SDK contract + Runtime compile |
| **Exists** | [`Assets/`](../Assets/) placeholder (empty shell) |
| **Not yet** | Game scenes, prefabs, ScriptableObject registries |
| **Not yet** | Platform quality service, Addressables setup, build scripts |
| **Not yet** | Android / Standalone Windows CI build wrappers |

This is an **SDK development shell** that can grow into a full **Android + PC** game in **one Unity project**. It is not a blank game and not a finished game.

---

## Fast Game stack (do not reimplement)

Generic mobile-first specs often assume a greenfield project. This repo already ships backend integration:

| Concern | Use |
|---------|-----|
| Auth, catalog, content, shop, ads | **`com.fastgame.sdk`** → [package README](../Packages/com.fastgame.sdk/README.md) |
| Scene wiring (UE-like) | **Fast Game → Client / Auth / Shop** components |
| Android store IAP | **FastGameStore** inside the package — one flavor **per APK**; use **Unlock Sku** only |
| Backend | [`fast-game`](../fast-game/Readme.md) dev stack (`api.localhost` → `http://api.localhost/api/v1`) |
| Multiplayer rooms | **Sibling** [Colyseus Unity SDK](https://github.com/colyseus/colyseus-unity-sdk) — `Scripts/fetch-colyseus.bat` → `Packages/io.colyseus.sdk/` |
| API contract | [`CONTRACT.md`](../CONTRACT.md) |

**Initialize Game** (once): `GameCode` + `StorePlatform` (store install check on device).  
**Initialize Client** (reconnect): `ApiBaseUrl`. Auth OTP / shop empty pins use Initialize Game — not per-button args.

Do **not** add a second HTTP client, shop stack, or Colyseus wrapper unless documented in `CONTRACT.md`.

---

## Evaluated production vision

The same **mobile-first, single-project** goals as [`fast-game-ue`](../fast-game-ue/docs/PROJECT.md) apply here: **Android + Windows**, shared gameplay, platform-specific presentation. Below: Unity-specific mapping.

### Keep (game production rules)

1. **One Unity project** for Android and PC — no separate “mobile port” project at the end.
2. **Shared gameplay** — one player controller / game manager; platform differences in quality, assets, and input — not `Player_Mobile` vs `Player_PC` duplicates.
3. **Central quality abstraction** — e.g. `PlatformQuality.GetAssetVariant()`, URP tier, particle budgets; avoid `#if UNITY_ANDROID` in every script.
4. **Asset variants only when worth it** — optional `_Mobile` / `_PC` meshes or LOD groups; not mandatory for every prop.
5. **Mobile-first rendering** — pick a mobile-safe pipeline (typically **URP**); validate every shader and feature on Android before relying on it for core gameplay.
6. **Textures & meshes** — ASTC / sensible max sizes on Android; lower poly LODs with **independent** mobile vs PC chains where it matters.
7. **Input abstraction** — actions (`Move`, `Interact`, …) mapped to touch, gamepad, and KBM via **Input System** (recommended) or a thin input facade.
8. **Budgets early** — target FPS, memory, draw calls, build size on a **defined Android device**; profile on device throughout milestones.
9. **Definition of done** — feature works on PC **and** target Android with acceptable performance.
10. **Continuous Android builds** — Development APK/AAB throughout; Shipping only at release gates.

### Defer (add when game work starts)

| Idea | Unity recommendation |
|------|----------------------|
| `PlatformQuality` service | `Assets/Core/Systems/` or small asmdef when first scenes exist |
| Logical asset ScriptableObjects | When environment catalog grows |
| `Scripts/build-android.sh` / `.bat` | Wrap `Unity -batchmode -buildTarget Android …` when packaging is routine |
| Build config JSON / CI matrix | With first GitLab/GitHub pipeline |
| Editor validation (poly count, missing mobile LOD) | Custom Editor window after `Assets/` grows |
| Addressables | When load times / memory require streaming — not required for tiny maps |
| Separate mobile scenes | Avoid — prefer shared scenes + quality tiers |

### Already solved

- Store billing / restore → **FastGameStore** + **FastGameShopBehaviour.UnlockSku**
- FastAPI / ENTER auth → **FastGameClient** + Auth components
- SDK tests → `py -3 run_tests.py`

---

## Current layout vs planned

```text
fast-game-unity/                       Today          When game lands
├── Packages/
│   ├── manifest.json                  ✓              ✓
│   └── com.fastgame.sdk/              ✓              ✓ (upstream SDK)
├── Assets/                            empty          game content (below)
├── ProjectSettings/                   minimal        URP, quality, Android, input
├── tests/                             ✓              ✓
├── run_tests.py                       ✓              ✓
├── CONTRACT.md                        ✓              ✓
├── Scripts/                           —              batchmode build entrypoints
└── docs/PROJECT.md                    ✓              this file
```

Suggested **Assets/** layout when you start:

```text
Assets/
├── Core/
│   ├── Gameplay/
│   ├── Characters/
│   ├── Systems/           # PlatformQuality, game state
│   ├── UI/                # wire Fast Game Auth/Shop or custom + components
│   ├── Input/             # Input Actions + Mobile / PC maps
│   └── Audio/
├── Environment/
├── Platform/              # optional mobile/PC prefab variants
├── Scenes/
├── Data/                  # ScriptableObjects — logical asset IDs
├── VFX/
├── Settings/              # URP assets, quality tiers
└── _Dev/                  # dev-only test scenes
```

Naming for variants: `Tree_01_Mobile`, `Tree_01_PC`; logical id `Tree_01` on a ScriptableObject.

---

## Android + PC targets

**Platforms:** Android + Windows Standalone from one project.

**Android store builds:**

- **Initialize Game → Store Platform** must match the APK (`myket` / `caffebazar` / `googleplay`).
- Copy **exactly one** flavor from [`FastGameStore/flavors/`](../Packages/com.fastgame.sdk/Plugins/Android/FastGameStore/flavors/) into `src/com/fastgame/store/` before build — see [store README](../Packages/com.fastgame.sdk/Plugins/Android/FastGameStore/README.md).
- Fast Game login need **not** match the store wallet email.

**Rendering (when URP is configured):**

- Mobile tier: limit real-time lights, shadow distance, post-processing, overdraw.
- Avoid desktop-only assumptions (heavy SSAO, multiple full-screen passes) in shared gameplay materials.
- Particle systems: use **limit over lifetime** / platform-scaled emission, not duplicate gameplay prefabs.

**UI:** uGUI / UI Toolkit layouts that work with touch; safe areas; no mouse-only flows in shared UI scripts.

---

## Build matrix (target state)

|  | Development | Release |
|--|-------------|---------|
| **Android** | Fast iteration APK | Store / AAB |
| **Windows** | Standalone dev exe | PC release |

Use Unity’s build pipeline (`BuildPipeline`, `-batchmode`, Build Profiles in 6.x). A thin `Scripts/` wrapper should call the **same args** locally and in CI.

Not implemented in-repo yet — Milestone 0 below.

---

## Milestones (adapted to this repo)

### Milestone 0 — Foundation (partial)

- [x] Unity 6.3 project opens; **com.fastgame.sdk** resolves from `manifest.json`
- [x] Samples importable; `run_tests.py` passes contract tests
- [ ] URP (or chosen pipeline) + Quality Settings tiers (Mobile / PC)
- [ ] First game scene (bootstrap + Fast Game Client object)
- [ ] Android Development build from CLI
- [ ] Windows Standalone Development build from CLI
- [ ] Documented target Android device(s)

### Milestone 1 — First playable

- [ ] Player + input (touch + KBM)
- [ ] **Fast Game → Client / Auth** wired; login smoke test against dev `fast-game`
- [ ] Same scene in Android + PC Development builds

### Milestone 2 — Vertical slice

- [ ] Auth → content/session → gameplay → shop (if applicable)
- [ ] Platform quality hooks + first asset variants where needed
- [ ] Profiling on target Android (Memory Profiler, Frame Debugger)
- [ ] Optional: Colyseus package for multiplayer slice

### Milestone 3 — Production / release

- [ ] Release builds via same scripts as dev
- [ ] Store APK per flavor with matching **StorePlatform**
- [ ] Performance, memory, load time, and build size within budgets

---

## Workflow

```text
Design → Implement → PC Play Mode → Android device test → Profile → Commit
```

Hard rule:

> If it cannot be built and tested on Android during development, it is not production-ready.

With Fast Game: API changes must pass **`fast-game` frontend** + **`fast-game-unity`** / **`fast-game-ue`** SDK tests ([`CONTRACT.md`](../CONTRACT.md)).

---

## Parity with Unreal

Keep **game design and backend usage aligned** with [`fast-game-ue`](../fast-game-ue/docs/PROJECT.md):

| Topic | Unity | Unreal |
|-------|-------|--------|
| Backend client | `com.fastgame.sdk` | `Plugins/FastGame` |
| Init split | Initialize Game / Client components | Subsystem nodes |
| Android IAP | FastGameStore in package | `Plugins/FastGameStore` |
| Multiplayer | Colyseus Unity SDK | colyseus-unreal |
| Contract | [`CONTRACT.md`](../CONTRACT.md) | same file in UE repo |

Same catalog `GameCode`, same shop flows, same ENTER auth routing — different engine presentation only.

---

## Architecture (target)

```text
ONE UNITY PROJECT (fast-game-unity)
        |
   GAMEPLAY (shared)          PLATFORM (presentation)
        |                            |
   Game scripts + SOs         Quality tiers / variants
   FastGame components              |
   Colyseus (optional)        URP / VFX / LOD / UI scale
        |                            |
        +---- Android APK / Windows exe ----+
                  (batchmode / CI)
```

PC is a **higher-quality view of the same game**, not a separate codebase downgraded for mobile at the end.

---

## Related docs

| Doc | Purpose |
|-----|---------|
| [README.md](../README.md) | Quick open + test |
| [Packages/com.fastgame.sdk/README.md](../Packages/com.fastgame.sdk/README.md) | Components + C# API |
| [CONTRACT.md](../CONTRACT.md) | HTTP + SDK contract |
| [FastGameStore README](../Packages/com.fastgame.sdk/Plugins/Android/FastGameStore/README.md) | Android IAP flavors |
| [fast-game-ue/docs/PROJECT.md](../fast-game-ue/docs/PROJECT.md) | UE sibling — shared production philosophy |
