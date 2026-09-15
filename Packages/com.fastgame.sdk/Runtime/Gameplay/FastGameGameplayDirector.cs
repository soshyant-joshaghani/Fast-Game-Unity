using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    /// <summary>
    /// Single LEVEL façade. Boots tip map profiles; owns V2 Character/Camera controllers.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Gameplay Director")]
    public sealed class FastGameGameplayDirector : MonoBehaviour
    {
        [Header("Client / Map")]
        public FastGameClientBehaviour ClientHost;
        public FastGameMapComponent Map;
        public FastGameLevelSceneBehaviour Level;

        [Header("Modules (V2)")]
        public FastGameCameraController CameraController;
        public FastGameCharacterController CharacterController;
        public FastGameAbilityRuntime AbilityRuntime;
        public FastGameParamRuntime ParamRuntime;
        public FastGameLootRuntime LootRuntime;
        public FastGameFlowRuntime FlowRuntime;
        public FastGameCharacterComponent PlayerEntity;

        [Header("Legacy aliases (G1)")]
        public FastGameCameraRuntime CameraRuntime;
        public FastGameMovementRuntime MovementRuntime;

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

            if (CameraController == null)
                CameraController = GetComponent<FastGameCameraController>()
                    ?? GetComponentInChildren<FastGameCameraController>(true);
            if (CameraController == null && CameraRuntime == null)
                CameraRuntime = GetComponent<FastGameCameraRuntime>()
                    ?? GetComponentInChildren<FastGameCameraRuntime>(true);
            if (CameraController == null && CameraRuntime != null)
                CameraController = CameraRuntime.Controller;

            if (CharacterController == null)
                CharacterController = GetComponentInChildren<FastGameCharacterController>(true);
            if (CharacterController == null && MovementRuntime == null)
                MovementRuntime = GetComponentInChildren<FastGameMovementRuntime>(true);
            if (CharacterController == null && MovementRuntime != null)
                CharacterController = MovementRuntime.Controller;

            if (AbilityRuntime == null)
                AbilityRuntime = GetComponentInChildren<FastGameAbilityRuntime>(true);
            if (ParamRuntime == null)
                ParamRuntime = GetComponentInChildren<FastGameParamRuntime>(true);
            if (LootRuntime == null)
                LootRuntime = GetComponentInChildren<FastGameLootRuntime>(true);
            if (FlowRuntime == null)
                FlowRuntime = GetComponentInChildren<FastGameFlowRuntime>(true);
            if (PlayerEntity == null)
                PlayerEntity = GetComponentInChildren<FastGameCharacterComponent>(true);

            if (CameraController != null && CameraController.FollowTarget == null && PlayerEntity != null)
                CameraController.SetFollowTarget(PlayerEntity.transform);
            if (FlowRuntime != null)
            {
                FlowRuntime.Director = this;
                FlowRuntime.Map = Map;
            }
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
                if (FlowRuntime != null)
                {
                    FlowRuntime.LoadFromMapTip(body);
                    FlowRuntime.BootFromLoadedTip();
                }
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

            // Prefer mode runtime_settings.camera_profile when present
            var modes = FastGameJson.GetArray(payload, "map_modes");
            if (modes != null && !string.IsNullOrWhiteSpace(Map?.ModeId))
            {
                for (var i = 0; i < modes.Count; i++)
                {
                    var mm = modes[i] as System.Collections.Generic.Dictionary<string, object>;
                    if (mm == null)
                        continue;
                    if (!string.Equals(
                            FastGameJson.GetString(mm, "mode_id"),
                            Map.ModeId,
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    var rs = FastGameJson.GetObject(mm, "runtime_settings");
                    var cam = FastGameJson.GetString(rs, "camera_profile");
                    if (!string.IsNullOrWhiteSpace(cam))
                        ActiveCameraProfile = cam;
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(ActiveCameraProfile))
                ApplyCameraProfile(ActiveCameraProfile);

            if (CharacterController != null && !string.IsNullOrWhiteSpace(ActiveMovementProfile))
                CharacterController.ApplyMovementProfile(ActiveMovementProfile);
        }

        public void ApplyMovementProfile(string profile)
        {
            ActiveMovementProfile = profile ?? "";
            CharacterController?.ApplyMovementProfile(ActiveMovementProfile);
            MovementRuntime?.ApplyMovementProfile(ActiveMovementProfile);
        }

        public void ApplyCameraProfile(string profile)
        {
            ActiveCameraProfile = profile ?? "";
            CameraController?.ApplyCameraProfile(ActiveCameraProfile);
            CameraRuntime?.ApplyCameraProfile(ActiveCameraProfile);
        }

        public void ActivateAbility(string abilityId) => AbilityRuntime?.ActivateAbility(abilityId);

        public void DeactivateAbility(string abilityId) => AbilityRuntime?.DeactivateAbility(abilityId);

        public void SetAnimator(string paramName, float value) => ParamRuntime?.SetAnimator(paramName, value);

        public void SetAnimator(string paramName, bool value) => ParamRuntime?.SetAnimator(paramName, value);

        public void SetMaterial(string paramName, float value) => ParamRuntime?.SetMaterial(paramName, value);

        public void NotifyTriggerEnter(string triggerId) => FlowRuntime?.NotifyTriggerEnter(triggerId);

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
