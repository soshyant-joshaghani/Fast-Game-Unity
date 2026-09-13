using System.Threading.Tasks;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// LEVEL — sync with fast-game map NAME. Delegates GetMapConfig / Travel Map to
    /// <see cref="FastGameMapComponent"/>; optional <see cref="FastGameGameplayDirector"/> boots tip profiles (G1).
    /// </summary>
    [AddComponentMenu("Fast Game/Scenes/Level")]
    public sealed class FastGameLevelSceneBehaviour : FastGameSceneFlowBehaviour
    {
        [Header("Map")]
        [Tooltip("Optional — uses FastGameMapComponent on this GameObject or children.")]
        public FastGameMapComponent Map;

        [Tooltip("Catalog map_id / NAME — forwarded to Map component.")]
        public string MapId;

        [Tooltip("Active mode NAME (solo, pvp, …) — forwarded to Map component.")]
        public string ModeId;

        [Header("Gameplay (G1)")]
        [Tooltip("Optional — boots camera/movement from map tip via GameplayDirector.")]
        public FastGameGameplayDirector Director;

        [Header("Navigation")]
        [Tooltip("Return to menu when level ends.")]
        public string MenuScene = FastGameSceneNames.Menu;

        void Awake()
        {
            AutoLoadNextOnComplete = false;
            ApplyToMapComponent();
            if (Director == null)
                Director = GetComponent<FastGameGameplayDirector>()
                    ?? GetComponentInChildren<FastGameGameplayDirector>(true);
        }

        void OnValidate()
        {
            ApplyToMapComponent();
        }

        void ApplyToMapComponent()
        {
            var map = ResolveMap();
            if (map == null)
                return;
            if (!string.IsNullOrWhiteSpace(MapId))
                map.MapId = MapId.Trim();
            if (!string.IsNullOrWhiteSpace(ModeId))
                map.ModeId = ModeId.Trim();
        }

        public FastGameMapComponent ResolveMap()
        {
            if (Map != null)
                return Map;
            Map = GetComponent<FastGameMapComponent>()
                ?? GetComponentInChildren<FastGameMapComponent>(true);
            return Map;
        }

        public FastGameGameplayDirector ResolveDirector()
        {
            if (Director != null)
                return Director;
            Director = GetComponent<FastGameGameplayDirector>()
                ?? GetComponentInChildren<FastGameGameplayDirector>(true);
            return Director;
        }

        public void BootGameplay() => ResolveDirector()?.Boot();

        public void ReturnToMenu()
        {
            if (!string.IsNullOrWhiteSpace(MenuScene))
                LoadScene(MenuScene);
        }

        public void GetMapConfig() => ResolveMap()?.GetMapConfig();

        public Task GetMapConfigAsync(string gameCode = null) =>
            ResolveMap()?.GetMapConfigAsync(gameCode) ?? Task.CompletedTask;

        public void TravelMap(string targetMapId = null) => ResolveMap()?.TravelMap(targetMapId);

        public Task TravelMapAsync(string targetMapId = null) =>
            ResolveMap()?.TravelMapAsync(targetMapId) ?? Task.CompletedTask;
    }
}
