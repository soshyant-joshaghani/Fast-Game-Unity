using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable]
    public class FastGameJsonFetchEvent : UnityEvent<bool, string, string> { }

    /// <summary>
    /// Binds a catalog character NAME on a prefab — fetches progressive tip via GetCharacter.
    /// Wire local mesh/anim from project assets; config comes from Fast-Game.
    /// </summary>
    [AddComponentMenu("Fast Game/Entity/Character")]
    public sealed class FastGameCharacterComponent : MonoBehaviour
    {
        [Header("Client")]
        [Tooltip("Leave empty to use FastGameClientBehaviour.Instance")]
        public FastGameClientBehaviour ClientHost;

        [Header("Character")]
        [Tooltip("Locale-free catalog NAME (e.g. PLAYER_SAMPLE).")]
        public string CharacterId;

        [Header("Events")]
        public FastGameJsonFetchEvent OnCharacterFetched;

        public string GetCharacterId() => CharacterId ?? "";

        /// <summary>Empty GameCode → Initialize Game GameCode.</summary>
        public void FetchCharacter() => _ = Run(FetchCharacterAsync);

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
                var json = FastGameJson.Stringify(body);
                OnCharacterFetched?.Invoke(true, json, "");
            }
            catch (Exception e)
            {
                OnCharacterFetched?.Invoke(false, "", e.Message);
            }
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
