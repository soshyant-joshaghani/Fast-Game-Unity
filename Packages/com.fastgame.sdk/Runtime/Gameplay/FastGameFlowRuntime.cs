using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable]
    public class FastGameFlowNodeEvent : UnityEvent<string, string> { }

    /// <summary>
    /// Walks compiled map_mode.flow_runtime (V3 hub macros).
    /// Boots on Director map tip; NotifyTriggerEnter for Doctor/doors.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Flow Runtime")]
    public sealed class FastGameFlowRuntime : MonoBehaviour
    {
        public FastGameClientBehaviour ClientHost;
        public FastGameMapComponent Map;
        public FastGameGameplayDirector Director;
        public FastGameDialoguePlayerComponent Dialogue;
        public FastGameCharacterComponent PlayerEntity;

        [Tooltip("When true, start on_level_started / on_start after LoadFromMapTip.")]
        public bool AutoStartLevelEvents = true;

        public UnityEvent OnFlowIdle;
        public FastGameFlowNodeEvent OnNodeExecuted;
        public UnityEvent<string> OnFlowFailed;
        public UnityEvent OnFellowUnlocked;

        readonly Dictionary<string, Dictionary<string, object>> _nodes =
            new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, object> _locals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _doOnce = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool _busy;
        string _mapId = "";

        public bool Busy => _busy;

        void Awake()
        {
            ResolveRefs();
        }

        void ResolveRefs()
        {
            if (Director == null)
                Director = GetComponent<FastGameGameplayDirector>()
                    ?? GetComponentInParent<FastGameGameplayDirector>();
            if (Map == null)
                Map = Director != null ? Director.Map : GetComponentInChildren<FastGameMapComponent>(true);
            if (Dialogue == null)
                Dialogue = GetComponentInChildren<FastGameDialoguePlayerComponent>(true);
            if (PlayerEntity == null)
                PlayerEntity = GetComponentInChildren<FastGameCharacterComponent>(true);
        }

        /// <summary>Load flow_runtime from GetMapConfig body (active mode preferred).</summary>
        public void LoadFromMapTip(Dictionary<string, object> mapTip)
        {
            _nodes.Clear();
            if (mapTip == null)
                return;
            var payload = FastGameJson.GetObject(mapTip, "payload") ?? mapTip;
            _mapId = FastGameJson.GetString(payload, "map_id") ?? Map?.MapId ?? "";

            var runtime = ResolveRuntime(payload);
            for (var i = 0; i < runtime.Count; i++)
            {
                var n = runtime[i] as Dictionary<string, object>;
                if (n == null)
                    continue;
                var id = FastGameJson.GetString(n, "node_id");
                if (!string.IsNullOrWhiteSpace(id))
                    _nodes[id.Trim()] = n;
            }
        }

        List<object> ResolveRuntime(Dictionary<string, object> payload)
        {
            var modes = FastGameJson.GetArray(payload, "map_modes");
            var modeId = Map != null ? Map.ModeId : "";
            if (modes != null && !string.IsNullOrWhiteSpace(modeId))
            {
                for (var i = 0; i < modes.Count; i++)
                {
                    var mm = modes[i] as Dictionary<string, object>;
                    if (mm == null)
                        continue;
                    if (!string.Equals(
                            FastGameJson.GetString(mm, "mode_id"),
                            modeId,
                            StringComparison.OrdinalIgnoreCase))
                        continue;
                    var fr = FastGameJson.GetArray(mm, "flow_runtime");
                    if (fr != null && fr.Count > 0)
                        return fr;
                }
            }
            return FastGameJson.GetArray(payload, "flow_runtime") ?? new List<object>();
        }

        public void BootFromLoadedTip()
        {
            if (!AutoStartLevelEvents)
                return;
            _ = StartEventKindsAsync("on_level_started", "on_start", "on_game_enter");
        }

        public void NotifyTriggerEnter(string triggerId) =>
            _ = StartTriggerAsync(triggerId);

        public void NotifyPlayerJoined() =>
            _ = StartEventKindsAsync("on_player_joined");

        async Task StartEventKindsAsync(params string[] kinds)
        {
            foreach (var kv in _nodes)
            {
                var kind = FastGameJson.GetString(kv.Value, "kind") ?? "";
                for (var i = 0; i < kinds.Length; i++)
                {
                    if (string.Equals(kind, kinds[i], StringComparison.OrdinalIgnoreCase))
                    {
                        await RunFromAsync(kv.Key);
                        break;
                    }
                }
            }
        }

        async Task StartTriggerAsync(string triggerId)
        {
            var want = (triggerId ?? "").Trim();
            if (string.IsNullOrEmpty(want))
                return;
            foreach (var kv in _nodes)
            {
                var kind = FastGameJson.GetString(kv.Value, "kind") ?? "";
                if (!string.Equals(kind, "on_trigger_enter", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(kind, "on_interact", StringComparison.OrdinalIgnoreCase))
                    continue;
                var p = FastGameJson.GetObject(kv.Value, "params") ?? new Dictionary<string, object>();
                var tid = FastGameJson.GetString(p, "trigger_id")
                    ?? FastGameJson.GetString(p, "placement_id")
                    ?? "";
                if (string.Equals(tid, want, StringComparison.OrdinalIgnoreCase))
                    await RunFromAsync(kv.Key);
            }
        }

        public async Task RunFromAsync(string nodeId)
        {
            if (_busy || string.IsNullOrWhiteSpace(nodeId))
                return;
            _busy = true;
            try
            {
                ResolveRefs();
                await WalkAsync(nodeId.Trim());
                OnFlowIdle?.Invoke();
            }
            catch (Exception e)
            {
                OnFlowFailed?.Invoke(e.Message);
                Debug.LogWarning("[FastGame Flow] " + e.Message, this);
            }
            finally
            {
                _busy = false;
            }
        }

        async Task WalkAsync(string nodeId)
        {
            var guard = 0;
            var current = nodeId;
            while (!string.IsNullOrEmpty(current) && guard++ < 256)
            {
                if (!_nodes.TryGetValue(current, out var node))
                    break;
                var kind = (FastGameJson.GetString(node, "kind") ?? "").Trim().ToLowerInvariant();
                OnNodeExecuted?.Invoke(current, kind);
                current = await ExecuteAsync(node, kind);
            }
        }

        async Task<string> ExecuteAsync(Dictionary<string, object> node, string kind)
        {
            var paramsObj = FastGameJson.GetObject(node, "params") ?? new Dictionary<string, object>();
            switch (kind)
            {
                case "on_level_started":
                case "on_start":
                case "on_game_enter":
                case "on_trigger_enter":
                case "on_interact":
                case "on_player_joined":
                case "get_player":
                case "get_character_info":
                case "assign_input":
                case "instantiate_character":
                    return FirstNext(node);

                case "set_var":
                    await SetVarAsync(
                        FastGameJson.GetString(paramsObj, "var_name"),
                        paramsObj.TryGetValue("value", out var sv) ? sv : true);
                    return FirstNext(node);

                case "branch":
                {
                    var cond = FastGameJson.GetString(paramsObj, "condition_var") ?? "";
                    var equals = FastGameJson.GetBool(paramsObj, "equals", true);
                    var val = await GetVarBoolAsync(cond);
                    var pin = (val == equals) ? "true" : "false";
                    return BranchNext(node, pin) ?? FirstNext(node);
                }

                case "sequence":
                    return FirstNext(node);

                case "do_once":
                {
                    var id = FastGameJson.GetString(node, "node_id") ?? "";
                    if (_doOnce.Contains(id))
                        return null;
                    _doOnce.Add(id);
                    return BranchNext(node, "completed") ?? FirstNext(node);
                }

                case "delay":
                {
                    var sec = FastGameJson.GetFloat(paramsObj, "seconds", 0.1f);
                    await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, sec)));
                    return BranchNext(node, "completed") ?? FirstNext(node);
                }

                case "play_dialogue":
                case "start_dialogue":
                {
                    var dlg = FastGameJson.GetString(paramsObj, "dialogue_id");
                    if (Dialogue == null)
                        Dialogue = gameObject.AddComponent<FastGameDialoguePlayerComponent>();
                    await Dialogue.PlayDialogueAsync(dlg);
                    await TryUnlockFellowAsync();
                    return FirstNext(node);
                }

                case "load_level":
                case "travel_map":
                {
                    var mapId = FastGameJson.GetString(paramsObj, "map_id");
                    if (Map != null && !string.IsNullOrWhiteSpace(mapId))
                        await Map.TravelMapAsync(mapId);
                    return BranchNext(node, "traveled") ?? FirstNext(node);
                }

                case "unlock_achievements":
                    await UnlockAchievementsAsync(paramsObj);
                    return FirstNext(node);

                case "set_camera_profile":
                {
                    var profile = FastGameJson.GetString(paramsObj, "profile")
                        ?? FastGameJson.GetString(paramsObj, "camera_profile");
                    Director?.ApplyCameraProfile(profile);
                    return FirstNext(node);
                }

                case "set_movement_profile":
                {
                    var profile = FastGameJson.GetString(paramsObj, "profile")
                        ?? FastGameJson.GetString(paramsObj, "movement_profile");
                    Director?.ApplyMovementProfile(profile);
                    return FirstNext(node);
                }

                case "activate_ability":
                    Director?.ActivateAbility(FastGameJson.GetString(paramsObj, "ability_id"));
                    return FirstNext(node);

                default:
                    // Literals / data nodes — skip exec
                    if (kind.StartsWith("literal_") || kind is "get_var" or "compare" or "math"
                        or "boolean_and" or "boolean_or" or "boolean_not")
                        return null;
                    return FirstNext(node);
            }
        }

        async Task SetVarAsync(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            _locals[name.Trim()] = value;
            var client = FastGameClientBehaviour.Instance?.Client
                ?? ClientHost?.Client;
            if (client == null)
                return;
            var code = client.Config.GameCode ?? "";
            if (string.IsNullOrWhiteSpace(code))
                return;
            var payload = new Dictionary<string, object> { [name.Trim()] = value };
            try
            {
                await client.Progress.SaveAsync(code, "flow.var", _mapId, payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FastGame Flow] set_var progress: " + e.Message, this);
            }
        }

        async Task<bool> GetVarBoolAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (_locals.TryGetValue(name.Trim(), out var local))
                return ToBool(local);
            var client = FastGameClientBehaviour.Instance?.Client
                ?? ClientHost?.Client;
            if (client == null)
                return false;
            try
            {
                var code = client.Config.GameCode ?? "";
                // Prefer game-scoped flags (dialogue effects), then map-scoped.
                foreach (var mid in new[] { "", _mapId })
                {
                    var prog = await client.Progress.GetAsync(code, mid);
                    var flags = FastGameJson.GetObject(prog, "flags");
                    if (flags != null && flags.TryGetValue(name.Trim(), out var v))
                        return ToBool(v);
                }
            }
            catch
            {
                // offline / unauthorized
            }
            return false;
        }

        async Task UnlockAchievementsAsync(Dictionary<string, object> paramsObj)
        {
            var client = FastGameClientBehaviour.Instance?.Client
                ?? ClientHost?.Client;
            if (client == null)
                return;
            var code = client.Config.GameCode ?? "";
            var ids = new List<string>();
            var arr = FastGameJson.GetArray(paramsObj, "achievement_ids")
                ?? FastGameJson.GetArray(paramsObj, "achievements");
            if (arr != null)
            {
                for (var i = 0; i < arr.Count; i++)
                {
                    var s = arr[i]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(s))
                        ids.Add(s);
                }
            }
            var single = FastGameJson.GetString(paramsObj, "achievement_id");
            if (!string.IsNullOrWhiteSpace(single))
                ids.Add(single.Trim());
            foreach (var id in ids)
            {
                try
                {
                    if (string.Equals(id, "ACH_FASTGAME_FELLOW", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(id, "ACH_WELCOME_FASTGAME", StringComparison.OrdinalIgnoreCase))
                    {
                        await client.Progress.SaveAsync(
                            code,
                            "achievement.unlock",
                            _mapId,
                            new Dictionary<string, object> { ["achievement_id"] = id });
                    }
                    else
                    {
                        await client.Shop.ClaimFreeAsync(code, "achievement", id);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[FastGame Flow] unlock " + id + ": " + e.Message, this);
                }
            }
        }

        async Task TryUnlockFellowAsync()
        {
            var a = await GetVarBoolAsync("AskBallistix");
            var b = await GetVarBoolAsync("AskEverything");
            var c = await GetVarBoolAsync("AskBash");
            var d = await GetVarBoolAsync("AskProgress");
            if (!(a && b && c && d))
                return;
            await UnlockAchievementsAsync(new Dictionary<string, object>
            {
                ["achievement_id"] = "ACH_FASTGAME_FELLOW",
            });
            OnFellowUnlocked?.Invoke();
        }

        static string FirstNext(Dictionary<string, object> node)
        {
            var next = FastGameJson.GetArray(node, "next");
            if (next != null && next.Count > 0)
                return next[0]?.ToString();
            return BranchNext(node, "exec_out");
        }

        static string BranchNext(Dictionary<string, object> node, string pin)
        {
            var branches = FastGameJson.GetObject(node, "branches");
            if (branches == null || string.IsNullOrEmpty(pin))
                return null;
            if (!branches.TryGetValue(pin, out var raw) || raw == null)
                return null;
            if (raw is List<object> list && list.Count > 0)
                return list[0]?.ToString();
            return raw.ToString();
        }

        static bool ToBool(object v)
        {
            if (v is bool b)
                return b;
            if (v == null)
                return false;
            var s = v.ToString();
            return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
