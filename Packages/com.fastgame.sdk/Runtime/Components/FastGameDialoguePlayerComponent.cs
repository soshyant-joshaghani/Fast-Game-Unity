using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [AddComponentMenu("Fast Game/Scenarios/Dialogue Player")]
    public sealed class FastGameDialoguePlayerComponent : MonoBehaviour
    {
        public string DialogueId;

        public UnityEvent<string> OnSuccess = new();
        public UnityEvent<string, string> OnFailed = new();

        public async Task PlayDialogueAsync(string dialogueId = null, CancellationToken ct = default)
        {
            var id = string.IsNullOrWhiteSpace(dialogueId) ? DialogueId : dialogueId;
            if (string.IsNullOrWhiteSpace(id))
            {
                OnFailed?.Invoke("", "DialogueId required");
                return;
            }
            var client = FastGameClientBehaviour.Instance?.Client;
            if (client == null)
            {
                OnFailed?.Invoke(id, "FastGame not initialized");
                return;
            }
            await Task.Yield();
            OnSuccess?.Invoke(id);
        }
    }
}
