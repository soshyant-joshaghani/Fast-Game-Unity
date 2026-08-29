using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FastGame
{
    [Serializable]
    public class FastGameSceneCompleteEvent : UnityEvent { }

    /// <summary>
    /// Scene traversal — set <see cref="NextScene"/> and call <see cref="CompleteScene"/> when done.
    /// </summary>
    [AddComponentMenu("Fast Game/Scene Flow")]
    public class FastGameSceneFlowBehaviour : MonoBehaviour
    {
        [Tooltip("Scene to open after CompleteScene when AutoLoadNextOnComplete is true.")]
        public string NextScene = FastGameSceneNames.Language;

        [Tooltip("Load NextScene automatically when CompleteScene runs.")]
        public bool AutoLoadNextOnComplete = true;

        [Header("Events")]
        public FastGameSceneCompleteEvent OnSceneComplete;

        public void CompleteScene()
        {
            OnSceneComplete?.Invoke();
            if (AutoLoadNextOnComplete && !string.IsNullOrWhiteSpace(NextScene))
                LoadScene(NextScene);
        }

        public void LoadNextScene() => LoadScene(NextScene);

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;
            SceneManager.LoadScene(sceneName);
        }
    }
}
