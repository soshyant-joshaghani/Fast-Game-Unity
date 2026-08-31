using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Binds a catalog avatar NAME on a prefab (portrait / collectibles UI).
    /// Ownership comes from Progress / Shop — display uses local project assets.
    /// </summary>
    [AddComponentMenu("Fast Game/Entity/Avatar")]
    public sealed class FastGameAvatarComponent : MonoBehaviour
    {
        [Tooltip("Locale-free catalog NAME.")]
        public string AvatarId;

        public string GetAvatarId() => AvatarId ?? "";
    }
}
