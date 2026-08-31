using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [AddComponentMenu("Fast Game/Scenarios/Quiz Player")]
    public sealed class FastGameQuizPlayerComponent : MonoBehaviour
    {
        public string QuizId;

        public UnityEvent<string> OnSuccess = new();
        public UnityEvent<string, string> OnFailed = new();

        public async Task PlayQuizAsync(string quizId = null, CancellationToken ct = default)
        {
            var id = string.IsNullOrWhiteSpace(quizId) ? QuizId : quizId;
            if (string.IsNullOrWhiteSpace(id))
            {
                OnFailed?.Invoke("", "QuizId required");
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
