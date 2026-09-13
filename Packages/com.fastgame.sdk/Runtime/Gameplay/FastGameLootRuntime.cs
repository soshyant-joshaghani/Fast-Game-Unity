using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    /// <summary>
    /// G2 LootRuntime — GetLootTable + validated OpenLoot / ClaimPickup.
    /// Server rolls only; presentation fires events (grants / world_spawns).
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Loot Runtime")]
    public sealed class FastGameLootRuntime : MonoBehaviour
    {
        public FastGameClientBehaviour ClientHost;
        public FastGameMapComponent Map;
        public FastGameParamRuntime Params;

        [Tooltip("Animator/material param written true when a chest opens (declared tip params).")]
        public string OpenParamName = "IsOpen";

        [Header("Events")]
        public FastGameJsonFetchEvent OnLootTableFetched;
        public FastGameJsonFetchEvent OnLootOpened;
        public UnityEvent OnLootFailed;
        public UnityEvent OnWorldSpawnRequested;

        public bool Busy { get; private set; }
        public string LastLootTableId { get; private set; } = "";

        void Awake()
        {
            if (Map == null)
                Map = GetComponent<FastGameMapComponent>()
                    ?? GetComponentInChildren<FastGameMapComponent>(true);
            if (Params == null)
                Params = GetComponent<FastGameParamRuntime>()
                    ?? GetComponentInChildren<FastGameParamRuntime>(true);
        }

        public void FetchLootTable(string lootTableId) => _ = FetchLootTableAsync(lootTableId);

        public async Task FetchLootTableAsync(string lootTableId, string gameCode = null)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, gameCode);
                var id = (lootTableId ?? "").Trim();
                if (string.IsNullOrEmpty(id))
                    throw new FastGameException("FastGame: loot_table_id is empty");
                var body = await client.Content.GetLootTableAsync(code, id);
                LastLootTableId = id;
                OnLootTableFetched?.Invoke(true, FastGameJson.Stringify(body), "");
            }
            catch (Exception e)
            {
                OnLootTableFetched?.Invoke(false, "", e.Message);
                OnLootFailed?.Invoke();
                Debug.LogWarning("[FastGame Loot] " + e.Message, this);
            }
            finally
            {
                Busy = false;
            }
        }

        /// <summary>Open chest by pickup/placement — POST loot-open (server roll).</summary>
        public void OpenLoot(string pickupId, string placementId = null, string lootTableId = null) =>
            _ = OpenLootAsync(pickupId, placementId, lootTableId);

        public async Task OpenLootAsync(
            string pickupId = null,
            string placementId = null,
            string lootTableId = null,
            string gameCode = null,
            string modeId = null)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, gameCode);
                var mapId = Map != null ? Map.MapId : "";
                var body = await client.Content.OpenLootAsync(
                    code,
                    mapId: mapId,
                    modeId: modeId,
                    pickupId: pickupId,
                    placementId: placementId,
                    lootTableId: lootTableId);

                if (!string.IsNullOrWhiteSpace(OpenParamName) && Params != null)
                    Params.SetAnimator(OpenParamName, true);

                var spawns = FastGameJson.GetArray(body, "world_spawns");
                if (spawns != null && spawns.Count > 0)
                    OnWorldSpawnRequested?.Invoke();

                OnLootOpened?.Invoke(true, FastGameJson.Stringify(body), "");
            }
            catch (Exception e)
            {
                OnLootOpened?.Invoke(false, "", e.Message);
                OnLootFailed?.Invoke();
                Debug.LogWarning("[FastGame Loot] " + e.Message, this);
            }
            finally
            {
                Busy = false;
            }
        }

        /// <summary>Legacy pickup-claim path (also server-authoritative when loot_table_id set).</summary>
        public void ClaimPickup(string pickupId, string placementId = null) =>
            _ = ClaimPickupAsync(pickupId, placementId);

        public async Task ClaimPickupAsync(string pickupId, string placementId = null, string gameCode = null)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, gameCode);
                var mapId = Map != null ? Map.MapId : "";
                var body = await client.Content.ClaimPickupAsync(code, mapId, pickupId, placementId);
                OnLootOpened?.Invoke(true, FastGameJson.Stringify(body), "");
            }
            catch (Exception e)
            {
                OnLootOpened?.Invoke(false, "", e.Message);
                OnLootFailed?.Invoke();
            }
            finally
            {
                Busy = false;
            }
        }

        static string ResolveGameCode(FastGameClient client, string gameCode)
        {
            var trimmed = (gameCode ?? "").Trim();
            return !string.IsNullOrEmpty(trimmed) ? trimmed : (client.Config.GameCode ?? "").Trim();
        }
    }
}
