# Unity samples

## ApiOnly

Attach `FastGameApiOnlySample`. Fast Game SDK only — no Colyseus.
Uses **Unlock Sku** / **Complete Unlock** (not obsolete Buy* / VerifyPending).
`ApiBaseUrl` default: `http://api.localhost/api/v1`.

## SandboxMultiplayer

1. Install Fast Game SDK + official [Colyseus Unity SDK](https://docs.colyseus.io/getting-started/unity-sdk/) (sibling).
2. Attach `FastGameSandboxMultiplayerSample`.
3. Step 1 uses FastAPI (`PrepareSession`). Step 2 documents Colyseus `JoinOrCreate` using `colyseus_room` + `GetGameServer`.
