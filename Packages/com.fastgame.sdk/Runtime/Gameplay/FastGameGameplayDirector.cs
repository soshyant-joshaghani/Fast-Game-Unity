using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    /// <summary>
    /// Single LEVEL façade (G1). Boots tip map profiles, owns runtime modules.
    /// Designers call Director methods / wire Flow — not ten competing Hosts.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Gameplay Director")]
    public sealed class FastGameGameplayDirector : MonoBehaviour
    {
        [Header("Client / Map")]
        public FastGameClientBehaviour ClientHost;
        public FastGameMapComponent Map;
        public FastGameLevelSceneBehaviour Level;

        [Header("Modules")]
        public FastGameCameraRuntime CameraRuntime;
        public FastGameMovementRuntime MovementRuntime;
        public FastGameAbilityRuntime AbilityRuntime;
        public FastGameParamRuntime ParamRuntime;
        public FastGameLootRuntime LootRuntime;
        public FastGameCharacterComponent PlayerEntity;

        [Header("Boot")]
        [Tooltip("Fetch GetMapConfig on Start and apply camera/input profile fields when present.")]
        public bool BootOnStart = true;

        [Header("Events")]
        public UnityEvent OnBootComplete;
        public UnityEvent<string> OnBootFailed;
        public FastGameJsonFetchEvent OnMapConfigApplied;

        public bool Busy { get; private set; }
        public string ActiveCameraProfile { get; private set; } = "";
        public string ActiveMovementProfile { get; private set; } = "";
        public string ActiveInputProfileId { get; private set; } = "";

        void Awake()
        {
            ResolveModules();
        }

        void Start()
        {
            if (BootOnStart)
                Boot();
        }

        public void ResolveModules()
        {
            if (Level == null)
                Level = GetComponent<FastGameLevelSceneBehaviour>()
                    ?? GetComponentInChildren<FastGameLevelSceneBehaviour>(true);
            if (Map == null)
                Map = Level != null
                    ? Level.ResolveMap()
                    : GetComponent<FastGameMapComponent>()
                        ?? GetComponentInChildren<FastGameMapComponent>(true);

            if (CameraRuntime == null)
                CameraRuntime = GetComponent<FastGameCameraRuntime>()
                    ?? GetComponentInChildren<FastGameCameraRuntime>(true);
            if (MovementRuntime == null)
                MovementRuntime = GetComponentInChildren<FastGameMovementRuntime>(true);
            if (AbilityRuntime == null)
                AbilityRuntime = GetComponentInChildren<FastGameAbilityRuntime>(true);
            if (ParamRuntime == null)
                ParamRuntime = GetComponentInChildren<FastGameParamRuntime>(true);
            if (LootRuntime == null)
                LootRuntime = GetComponentInChildren<FastGameLootRuntime>(true);
            if (PlayerEntity == null)
                PlayerEntity = GetComponentInChildren<FastGameCharacterComponent>(true);

            if (CameraRuntime != null && CameraRuntime.FollowTarget == null && PlayerEntity != null)
                CameraRuntime.SetFollowTarget(PlayerEntity.transform);
        }

        /// <summary>Fetch map tip and apply camera_profile / input_profile_id / movement defaults.</summary>
        public void Boot() => _ = BootAsync();

        public async Task BootAsync(string gameCode = null)
        {
            if (Busy)
                return;
            Busy = true;
            try
            {
                ResolveModules();
                if (Map == null)
                    throw new FastGameException("FastGame: GameplayDirector needs a Map component");

                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, gameCode);
                if (string.IsNullOrWhiteSpace(Map.MapId))
                    throw new FastGameException("FastGame: MapId is not set");
                if (string.IsNullOrWhiteSpace(code))
                    throw new FastGameException("FastGame: GameCode is empty — Initialize Game first");

                var body = await client.Content.GetMapConfigAsync(code, Map.MapId.Trim());
                ApplyMapConfig(body);
                OnMapConfigApplied?.Invoke(true, FastGameJson.Stringify(body), "");
                OnBootComplete?.Invoke();
            }
            catch (Exception e)
            {
                OnMapConfigApplied?.Invoke(false, "", e.Message);
                OnBootFailed?.Invoke(e.Message);
                Debug.LogWarning("[FastGame Director] " + e.Message, this);
            }
            finally
            {
                Busy = false;
            }
        }

        public void ApplyMapConfig(System.Collections.Generic.Dictionary<string, object> root)
        {
            if (root == null)
                return;

            var payload = FastGameJson.GetObject(root, "payload") ?? root;
            ActiveCameraProfile = FastGameJson.GetString(payload, "camera_profile") ?? ActiveCameraProfile;
            ActiveInputProfileId = FastGameJson.GetString(payload, "input_profile_id") ?? ActiveInputProfileId;

            if (!string.IsNullOrWhiteSpace(ActiveCameraProfile) && CameraRuntime != null)
                CameraRuntime.ApplyCameraProfile(ActiveCameraProfile);

            // movement_profile often lives on the player entity tip — keep map override optional later
            if (MovementRuntime != null && !string.IsNullOrWhiteSpace(ActiveMovementProfile))
                MovementRuntime.ApplyMovementProfile(ActiveMovementProfile);
        }

        public void ApplyMovementProfile(string profile)
        {
            ActiveMovementProfile = profile ?? "";
            MovementRuntime?.ApplyMovementProfile(ActiveMovementProfile);
        }

        public void ApplyCameraProfile(string profile)
        {
            ActiveCameraProfile = profile ?? "";
            CameraRuntime?.ApplyCameraProfile(ActiveCameraProfile);
        }

        public void ActivateAbility(string abilityId) => AbilityRuntime?.ActivateAbility(abilityId);

        public void DeactivateAbility(string abilityId) => AbilityRuntime?.DeactivateAbility(abilityId);

        public void SetAnimator(string paramName, float value) => ParamRuntime?.SetAnimator(paramName, value);

        public void SetAnimator(string paramName, bool value) => ParamRuntime?.SetAnimator(paramName, value);

        public void SetMaterial(string paramName, float value) => ParamRuntime?.SetMaterial(paramName, value);

        public void OpenLoot(string pickupId, string placementId = null, string lootTableId = null) =>
            LootRuntime?.OpenLoot(pickupId, placementId, lootTableId);

        public void FetchLootTable(string lootTableId) => LootRuntime?.FetchLootTable(lootTableId);

        public void FetchPlayerCharacter() => PlayerEntity?.FetchCharacter();

        static string ResolveGameCode(FastGameClient client, string gameCode)
        {
            var trimmed = (gameCode ?? "").Trim();
            return !string.IsNullOrEmpty(trimmed) ? trimmed : (client.Config.GameCode ?? "").Trim();
        }
    }
}
