# Fast Game Unity

Standalone **Unity 6.3 LTS** project — Fast Game client kit (**parity with UE**).

**Role:** art + thin runtime interpreter. Prefabs hold meshes/animators + FastGame components; tip from [`fast-game`](../fast-game/README.md) drives camera, character/vehicle controllers, abilities, zones, input, and variable bindings. Keep binding **names** in sync. Do not invent Unity-only gameplay rules — match [`fast-game-ue`](../fast-game-ue/README.md) policy.

| Item | Path |
|------|------|
| UPM package | `Packages/com.fastgame.sdk` |
| Android IAP | `Packages/com.fastgame.sdk/Plugins/Android/FastGameStore` |
| Samples | Package Manager → **Fast Game SDK → Samples** |
| SDK docs | [Packages/com.fastgame.sdk/README.md](Packages/com.fastgame.sdk/README.md) |
| Project guide | [docs/PROJECT.md](docs/PROJECT.md) |
| Contract | [CONTRACT.md](CONTRACT.md) |
| Backend kit | [`../fast-game/`](../fast-game/README.md) |
| Plans | [`../fast-game/__plans__/`](../fast-game/__plans__/) |

## Open

1. Unity Hub → **Unity 6.3 LTS** → Open this folder
2. Hub may regenerate missing `ProjectSettings` on first import
3. Point **Initialize Client** at a running Fast Game API, e.g. `http://api.localhost/api/v1`

## Tests

```bat
py -3 run_tests.py
```

Contract + Unity Runtime `csc` + Android store compile checks under `tests/`.

## Multiplayer (Colyseus sibling)

Fast Game does **not** include Colyseus. Fetch the UPM package, then reopen the project:

```bat
Scripts\fetch-colyseus.bat
Scripts\fetch-colyseus.bat -Update
```

| Item | Detail |
|------|--------|
| Upstream | [colyseus/colyseus-unity-sdk](https://github.com/colyseus/colyseus-unity-sdk) |
| Installed to | `Packages/io.colyseus.sdk/` (gitignored; wired in `manifest.json`) |
| Pin / update | `Scripts/colyseus.lock.json` · `-Update` pulls latest on locked branch |

Includes NativeWebSocket fetch (same as upstream `unity-setup.sh`). See sample **SandboxMultiplayer**.
