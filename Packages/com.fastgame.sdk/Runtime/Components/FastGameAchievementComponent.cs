using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Binds a catalog achievement NAME on a prefab (trophy / inspect UI hook).
    /// </summary>
    [AddComponentMenu("Fast Game/Entity/Achievement")]
    public sealed class FastGameAchievementComponent : MonoBehaviour
    {
        [Tooltip("Locale-free catalog NAME.")]
        public string AchievementId;

        public string GetAchievementId() => AchievementId ?? "";
    }
}
