using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Binds a catalog title NAME on a prefab (profile / menu display).
    /// </summary>
    [AddComponentMenu("Fast Game/Entity/Title")]
    public sealed class FastGameTitleComponent : MonoBehaviour
    {
        [Tooltip("Locale-free catalog NAME.")]
        public string TitleId;

        public string GetTitleId() => TitleId ?? "";
    }
}
