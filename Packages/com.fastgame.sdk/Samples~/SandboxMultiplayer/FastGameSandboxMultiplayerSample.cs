using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame;
using FastGame.Models;
using UnityEngine;

namespace FastGame.Samples
{
    /// <summary>
    /// Multiplayer sample: Fast Game SDK (FastAPI) + sibling Colyseus Unity SDK.
    /// Install https://docs.colyseus.io/getting-started/unity-sdk/ separately — do not wrap Colyseus inside FastGame.
    /// </summary>
    public sealed class FastGameSandboxMultiplayerSample : MonoBehaviour
    {
        public string ApiBaseUrl = "api.localhost";
        public string GameServerUrlOverride = "";
        public string Identity = "admin@example.com";
        public string Password = "changethis";
        public string GameId = "sandbox-capsule";
        public string ModeId = "sandbox";
        public string MapId = "box-arena";

        FastGameClient _client;
        PreparedSession _session;
        string _status = "idle";

        // Opaque room from Colyseus sibling SDK (set after you join).
        object _colyseusRoom;

        void Awake()
        {
            _client = new FastGameClient(new FastGameConfig { ApiBaseUrl = ApiBaseUrl });
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 460, 300));
            GUILayout.Label("Fast Game + Colyseus (sibling SDKs)");
            GUILayout.Label(_status);
            GUILayout.Label("Room held: " + (_colyseusRoom != null));

            if (GUILayout.Button("1. Login + PrepareSession (FastAPI)"))
                _ = PrepareFlow();

            if (GUILayout.Button("2. Join via Colyseus SDK (sibling)"))
                _ = JoinColyseusFlow();

            if (GUILayout.Button("Send move / score / finish"))
                GUILayout.Label("Call room.Send from Colyseus SDK — see sample comments.");

            if (GUILayout.Button("Leave (Colyseus)"))
                _ = LeaveColyseusFlow();

            GUILayout.EndArea();
        }

        async Task PrepareFlow()
        {
            try
            {
                await _client.Auth.LoginAsync(Identity, Password);
                _session = await _client.Content.PrepareSessionAsync(GameId, ModeId, MapId);
                _status = $"prepared roomName={_session.ColyseusRoom} (join with Colyseus next)";
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task JoinColyseusFlow()
        {
            try
            {
                if (_session == null)
                    _session = await _client.Content.PrepareSessionAsync(GameId, ModeId, MapId);

                var endpoint = GameServerUrlOverride;
                if (string.IsNullOrEmpty(endpoint))
                {
                    var info = await _client.Catalog.GetGameServerAsync();
                    endpoint = info?.Url;
                }
                if (string.IsNullOrEmpty(endpoint))
                    throw new FastGameException("No game server URL");

                var roomName = string.IsNullOrEmpty(_session.ColyseusRoom)
                    ? "sandbox_room"
                    : _session.ColyseusRoom;

                // Sibling Colyseus SDK — install com.colyseus.sdk / official Unity package, then:
                //
                //   var coly = new Colyseus.ColyseusClient(endpoint);
                //   _colyseusRoom = await coly.JoinOrCreate<object>(roomName, new Dictionary<string, object> {
                //       { "gameId", GameId }, { "modeId", ModeId }, { "mapId", MapId }
                //   });
                //   // room.Send("move", new { x, y, z }); room.Send("score", new { delta = 1 }); …
                //
                _status =
                    $"Ready to Colyseus.JoinOrCreate(\"{roomName}\") @ {endpoint} — " +
                    "uncomment Colyseus code in JoinColyseusFlow after adding the sibling package.";
                await Task.CompletedTask;
            }
            catch (System.Exception e) { _status = e.Message; }
        }

        async Task LeaveColyseusFlow()
        {
            // await ((ColyseusRoom<object>)_colyseusRoom).Leave();
            _colyseusRoom = null;
            _status = "left (clear local handle; call Colyseus Leave when wired)";
            await Task.CompletedTask;
        }
    }
}
