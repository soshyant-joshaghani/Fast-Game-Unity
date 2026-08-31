using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FastGame
{
    /// <summary>Travel Map exec pins — mirrors UE <c>EFastGameTravelMapPin</c> DisplayNames.</summary>
    public enum FastGameTravelMapPin
    {
        Traveled,
        Matchmaking,
        WaitingHere,
        Failed,
    }

    /// <summary>ON_QUEST listener exec pins — mirrors UE <c>EFastGameQuestPin</c> DisplayNames.</summary>
    public enum FastGameQuestPin
    {
        Complete,
        Failed,
        NotStartedYet,
    }

    [Serializable]
    public class FastGameTravelMapSeatEvent : UnityEvent<SeatMintResult> { }

    [Serializable]
    public class FastGameQuestIdEvent : UnityEvent<string> { }

    /// <summary>
    /// Binds map + mode NAMEs on a level prefab — GetMapConfig, Travel Map (Flow action),
    /// and ON_QUEST listener events.
    /// </summary>
    [AddComponentMenu("Fast Game/Entity/Map")]
    public sealed class FastGameMapComponent : MonoBehaviour
    {
        [Header("Client")]
        [Tooltip("Leave empty to use FastGameClientBehaviour.Instance")]
        public FastGameClientBehaviour ClientHost;

        [Header("Map")]
        [Tooltip("Locale-free catalog NAME for this map (e.g. MAP_LEVEL_SAMPLE).")]
        public string MapId;

        [Tooltip("Active mode NAME (e.g. solo, pvp). Empty → solo/offline travel.")]
        public string ModeId;

        [Header("Events — Get Map Config")]
        public FastGameJsonFetchEvent OnMapConfigFetched;

        [Header("Events — Travel Map")]
        public UnityEvent OnTraveled;
        public FastGameTravelMapSeatEvent OnMatchmaking;
        public UnityEvent OnWaitingHere;
        public FastGameStringEvent OnFailed;

        [Header("Events — ON_QUEST listener")]
        public FastGameQuestIdEvent OnQuestComplete;
        public FastGameQuestIdEvent OnQuestFailed;
        public FastGameQuestIdEvent OnQuestNotStarted;

        public bool Busy { get; private set; }

        public string GetMapId() => MapId ?? "";
        public string GetModeId() => ModeId ?? "";

        /// <summary>Empty GameCode → Initialize Game GameCode. Uses component MapId.</summary>
        public void GetMapConfig() => _ = Run(GetMapConfigAsync);

        public async Task GetMapConfigAsync(string gameCode = null)
        {
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, gameCode);
                if (string.IsNullOrWhiteSpace(MapId))
                    throw new FastGameException("FastGame: MapId is not set");
                if (string.IsNullOrWhiteSpace(code))
                    throw new FastGameException("FastGame: GameCode is empty — call Initialize Game first");

                var body = await client.Content.GetMapConfigAsync(code, MapId.Trim());
                var json = FastGameJson.Stringify(body);
                OnMapConfigFetched?.Invoke(true, json, "");
            }
            catch (Exception e)
            {
                OnMapConfigFetched?.Invoke(false, "", e.Message);
            }
        }

        /// <summary>
        /// Travel Map Flow action. Empty targetMapId → component MapId.
        /// Events: Traveled | Matchmaking | Waiting Here | Failed.
        /// </summary>
        public void TravelMap(string targetMapId = null) => _ = Run(() => TravelMapAsync(targetMapId));

        public async Task TravelMapAsync(string targetMapId = null)
        {
            try
            {
                var client = FastGameClientBehaviour.RequireClient(ClientHost);
                var code = ResolveGameCode(client, null);
                if (string.IsNullOrWhiteSpace(code))
                    throw new FastGameException("FastGame: GameCode is empty — call Initialize Game first");

                var target = string.IsNullOrWhiteSpace(targetMapId) ? MapId : targetMapId.Trim();
                if (string.IsNullOrWhiteSpace(target))
                    throw new FastGameException("FastGame: TargetMapId is not set");

                var mode = (ModeId ?? "").Trim();
                var online = IsOnlineMode(mode);
                var sameMap = !string.IsNullOrWhiteSpace(MapId)
                    && string.Equals(target, MapId.Trim(), StringComparison.OrdinalIgnoreCase);

                if (sameMap)
                {
                    if (online)
                        await MintSeatAsync(client, code, target, mode, FastGameTravelMapPin.WaitingHere);
                    else
                        InvokeTravelPin(FastGameTravelMapPin.WaitingHere, null, "");
                    return;
                }

                if (online)
                {
                    await MintSeatAsync(client, code, target, mode, FastGameTravelMapPin.Matchmaking);
                    return;
                }

                var config = await client.Content.GetMapConfigAsync(code, target);
                var engineScene = ExtractEngineScene(config);
                if (string.IsNullOrWhiteSpace(engineScene))
                    throw new FastGameException("FastGame: engine_scene is not configured for target map");

                SceneManager.LoadScene(engineScene);
                InvokeTravelPin(FastGameTravelMapPin.Traveled, null, "");
            }
            catch (Exception e)
            {
                InvokeTravelPin(FastGameTravelMapPin.Failed, null, e.Message);
            }
        }

        /// <summary>Flow driver / kernel hook — fire ON_QUEST Complete pin.</summary>
        public void NotifyQuestComplete(string questId) => OnQuestComplete?.Invoke(questId ?? "");

        public void NotifyQuestFailed(string questId) => OnQuestFailed?.Invoke(questId ?? "");

        public void NotifyQuestNotStartedYet(string questId) => OnQuestNotStarted?.Invoke(questId ?? "");

        async Task MintSeatAsync(
            FastGameClient client,
            string gameCode,
            string mapId,
            string modeId,
            FastGameTravelMapPin successPin)
        {
            try
            {
                var seat = await client.Realtime.JoinMapAsync(
                    gameCode,
                    mapId,
                    string.IsNullOrEmpty(modeId) ? null : modeId);
                InvokeTravelPin(successPin, seat, "");
            }
            catch (Exception e)
            {
                InvokeTravelPin(FastGameTravelMapPin.Failed, null, e.Message);
            }
        }

        void InvokeTravelPin(FastGameTravelMapPin pin, SeatMintResult seat, string message)
        {
            switch (pin)
            {
                case FastGameTravelMapPin.Traveled:
                    OnTraveled?.Invoke();
                    break;
                case FastGameTravelMapPin.Matchmaking:
                    OnMatchmaking?.Invoke(seat ?? new SeatMintResult());
                    break;
                case FastGameTravelMapPin.WaitingHere:
                    OnWaitingHere?.Invoke();
                    break;
                default:
                    OnFailed?.Invoke(message ?? "");
                    break;
            }
        }

        static string ResolveGameCode(FastGameClient client, string gameCode)
        {
            var trimmed = (gameCode ?? "").Trim();
            return !string.IsNullOrEmpty(trimmed) ? trimmed : (client.Config.GameCode ?? "").Trim();
        }

        static bool IsOnlineMode(string modeId)
        {
            var trimmed = (modeId ?? "").Trim();
            return !string.IsNullOrEmpty(trimmed)
                && !trimmed.Equals("solo", StringComparison.OrdinalIgnoreCase);
        }

        static string ExtractEngineScene(Dictionary<string, object> root)
        {
            if (root == null)
                return "";

            var payload = FastGameJson.GetObject(root, "payload");
            if (payload != null)
            {
                var scene = FastGameJson.GetString(payload, "engine_scene");
                if (!string.IsNullOrWhiteSpace(scene))
                    return scene;
            }

            return FastGameJson.GetString(root, "engine_scene") ?? "";
        }

        async Task Run(Func<Task> action)
        {
            if (Busy)
                return;
            Busy = true;
            try
            {
                await action();
            }
            finally
            {
                Busy = false;
            }
        }
    }
}
