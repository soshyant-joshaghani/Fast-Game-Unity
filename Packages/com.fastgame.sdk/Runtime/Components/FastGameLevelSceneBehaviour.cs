using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// LEVEL — sync with fast-game map NAME. Wire scenario triggers here (GetMapConfig in B1d).
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Level")]
    public sealed class FastGameLevelSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Tooltip("Catalog map_id / NAME from published tip.")]
        public string MapId;

        [Tooltip("Return to menu when level ends.")]
        public string MenuScene = FastGameSceneNames.Menu;

        void Awake()
        {
            AutoLoadNextOnComplete = false;
        }

        public void ReturnToMenu()
        {
            if (!string.IsNullOrWhiteSpace(MenuScene))
                LoadScene(MenuScene);
        }
    }
}
