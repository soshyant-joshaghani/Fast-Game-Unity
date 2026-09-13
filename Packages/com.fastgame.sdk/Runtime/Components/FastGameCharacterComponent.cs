using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable]
    public class FastGameJsonFetchEvent : UnityEvent<bool, string, string> { }

    /// <summary>
    /// Binds a catalog entity/character NAME on a prefab — fetches progressive tip via GetCharacter.
    /// Wire local mesh/anim from project assets; config comes from Fast-Game.
    /// G1: optional ParamRuntime + tip movement/camera profile apply via Director.
    /// </summary>
    [AddComponentMenu("Fast Game/Entity/Character")]
    public sealed class FastGameCharacterComponent : MonoBehaviour
    {
        [Header("Client")]
        [Tooltip("Leave empty to use FastGameClientBehaviour.Instance")]
        public FastGameClientBehaviour ClientHost;

        [Header("Entity")]
        [Tooltip("Locale-free catalog NAME (e.g. PLAYER_SAMPLE). Alias: entity_id.")]
        public string CharacterId;

        [Tooltip("Entity kind from tip (character | vehicle | …). Default character.")]
        public string EntityKind = "character";

        [Tooltip("Optional prefab key from tip (documentation / Spawner).")]
        public string PrefabKey;

        [Header("Params (G1)")]
        public FastGameParamRuntime Params;
        public FastGameDeclaredParam[] DeclaredParams = Array.Empty<FastGameDeclaredParam>();

        [Header("Events")]
        public FastGameJsonFetchEvent OnCharacterFetched;

        public string GetCharacterId() => CharacterId ?? "";
        public string GetEntityId() => CharacterId ?? "";
        public string GetEntityKind() => EntityKind ?? "character";

        /// <summary>Empty GameCode → Initialize Game GameCode.</summary>
        public void FetchCharacter() => _ = Run(() => FetchCharacterAsync());

        public async Task FetchCharacterAsync(string gameCode = null)
        {
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, gameCode);
                if (string.IsNullOrWhiteSpace(CharacterId))
                    throw new FastGameException("FastGame: CharacterId is not set");
                if (string.IsNullOrWhiteSpace(code))
                    throw new FastGameException("FastGame: GameCode is empty — call Initialize Game first");

                var body = await client.Content.GetCharacterAsync(code, CharacterId.Trim());
                ApplyEntityTip(body);
                var json = FastGameJson.Stringify(body);
                OnCharacterFetched?.Invoke(true, json, "");
            }
            catch (Exception e)
            {
                OnCharacterFetched?.Invoke(false, "", e.Message);
            }
        }

        /// <summary>Apply movement/camera profile strings and prefab from tip payload when present.</summary>
        public void ApplyEntityTip(System.Collections.Generic.Dictionary<string, object> root)
        {
            if (root == null)
                return;
            var payload = FastGameJson.GetObject(root, "payload") ?? root;

            var kind = FastGameJson.GetString(payload, "kind");
            if (!string.IsNullOrWhiteSpace(kind))
                EntityKind = kind.Trim();

            var prefab = FastGameJson.GetString(payload, "prefab");
            if (!string.IsNullOrWhiteSpace(prefab))
                PrefabKey = prefab.Trim();

            var move = FastGameJson.GetString(payload, "movement_profile");
            var cam = FastGameJson.GetString(payload, "camera_profile");
            var director = GetComponentInParent<FastGameGameplayDirector>();
            if (director != null)
            {
                if (!string.IsNullOrWhiteSpace(move))
                    director.ApplyMovementProfile(move);
                if (!string.IsNullOrWhiteSpace(cam))
                    director.ApplyCameraProfile(cam);
            }
            else
            {
                var movement = GetComponent<FastGameMovementRuntime>();
                if (movement != null && !string.IsNullOrWhiteSpace(move))
                    movement.ApplyMovementProfile(move);
                var camera = UnityEngine.Object.FindObjectOfType<FastGameCameraRuntime>();
                if (camera != null && !string.IsNullOrWhiteSpace(cam))
                    camera.ApplyCameraProfile(cam);
            }

            if (Params == null)
                Params = GetComponent<FastGameParamRuntime>();
        }

        static string ResolveGameCode(FastGameClient client, string gameCode)
        {
            var trimmed = (gameCode ?? "").Trim();
            return !string.IsNullOrEmpty(trimmed) ? trimmed : (client.Config.GameCode ?? "").Trim();
        }

        async Task Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FastGame Character] " + e.Message, this);
            }
        }
    }
}
