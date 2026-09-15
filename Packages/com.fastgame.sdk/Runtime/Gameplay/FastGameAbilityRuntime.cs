using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable]
    public class FastGameAbilityDef
    {
        [Tooltip("Ability NAME from tip / catalog.")]
        public string AbilityId;

        [Tooltip("Param writes applied on activate (SetAnimator / SetMaterial).")]
        public FastGameParamWrite[] OnActivate = Array.Empty<FastGameParamWrite>();

        [Tooltip("Param writes applied on deactivate.")]
        public FastGameParamWrite[] OnDeactivate = Array.Empty<FastGameParamWrite>();

        public bool Toggle;
    }

    /// <summary>Tip-fed ability activate → ParamRuntime writes + binds (V2).</summary>
    [AddComponentMenu("Fast Game/Gameplay/Ability Runtime")]
    public sealed class FastGameAbilityRuntime : MonoBehaviour
    {
        public FastGameParamRuntime Params;
        public List<FastGameAbilityDef> Abilities = new List<FastGameAbilityDef>();

        public UnityEvent<string> OnAbilityActivated;
        public UnityEvent<string> OnAbilityDeactivated;
        public UnityEvent<string> OnAbilityFailed;

        readonly HashSet<string> _active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Awake()
        {
            if (Params == null)
                Params = GetComponent<FastGameParamRuntime>()
                    ?? GetComponentInChildren<FastGameParamRuntime>(true);
        }

        public bool IsActive(string abilityId) =>
            !string.IsNullOrWhiteSpace(abilityId) && _active.Contains(abilityId.Trim());

        /// <summary>Replace defs from character tip abilities[] (on_activate / on_deactivate / toggle).</summary>
        public void LoadFromTipAbilities(IList<object> abilities)
        {
            if (abilities == null)
                return;
            Abilities.Clear();
            for (var i = 0; i < abilities.Count; i++)
            {
                var row = abilities[i] as Dictionary<string, object>;
                if (row == null)
                    continue;
                var id = FastGameJson.GetString(row, "ability_id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                var p = FastGameJson.GetObject(row, "params") ?? new Dictionary<string, object>();
                var def = new FastGameAbilityDef
                {
                    AbilityId = id.Trim(),
                    Toggle = FastGameJson.GetBool(p, "toggle", false)
                        || string.Equals(
                            FastGameJson.GetString(row, "kind"),
                            "toggle",
                            StringComparison.OrdinalIgnoreCase),
                    OnActivate = ParseWrites(FastGameJson.GetArray(p, "on_activate")),
                    OnDeactivate = ParseWrites(FastGameJson.GetArray(p, "on_deactivate")),
                };
                Abilities.Add(def);
            }
        }

        public void ActivateAbility(string abilityId)
        {
            var id = (abilityId ?? "").Trim();
            if (string.IsNullOrEmpty(id))
            {
                OnAbilityFailed?.Invoke("empty ability id");
                return;
            }

            var def = Find(id);
            if (def == null)
            {
                // Tip may omit defs for engine-native locomotion ids — still signal success.
                if (IsBuiltinLocomotion(id))
                {
                    _active.Add(id);
                    OnAbilityActivated?.Invoke(id);
                    return;
                }
                OnAbilityFailed?.Invoke("unknown ability " + id);
                return;
            }

            if (def.Toggle && _active.Contains(id))
            {
                DeactivateAbility(id);
                return;
            }

            if (Params != null && def.OnActivate != null)
                Params.ApplyMany(def.OnActivate);

            _active.Add(id);
            OnAbilityActivated?.Invoke(id);
        }

        public void DeactivateAbility(string abilityId)
        {
            var id = (abilityId ?? "").Trim();
            if (string.IsNullOrEmpty(id) || !_active.Remove(id))
                return;

            var def = Find(id);
            if (Params != null && def?.OnDeactivate != null)
                Params.ApplyMany(def.OnDeactivate);

            OnAbilityDeactivated?.Invoke(id);
        }

        FastGameAbilityDef Find(string id)
        {
            for (var i = 0; i < Abilities.Count; i++)
            {
                var a = Abilities[i];
                if (a != null && string.Equals(a.AbilityId?.Trim(), id, StringComparison.OrdinalIgnoreCase))
                    return a;
            }
            return null;
        }

        static bool IsBuiltinLocomotion(string id) =>
            string.Equals(id, "move", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "jump", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "crouch", StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "sprint", StringComparison.OrdinalIgnoreCase);

        static FastGameParamWrite[] ParseWrites(List<object> rows)
        {
            if (rows == null || rows.Count == 0)
                return Array.Empty<FastGameParamWrite>();
            var list = new List<FastGameParamWrite>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i] as Dictionary<string, object>;
                if (row == null)
                    continue;
                var name = FastGameJson.GetString(row, "name");
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var channel = ParseChannel(FastGameJson.GetString(row, "channel"));
                var type = ParseType(FastGameJson.GetString(row, "type"));
                var write = new FastGameParamWrite
                {
                    Name = name.Trim(),
                    Channel = channel,
                    Type = type,
                };
                if (row.TryGetValue("value", out var raw) && raw != null)
                {
                    switch (type)
                    {
                        case FastGameParamValueType.Bool:
                            write.BoolValue = Convert.ToBoolean(raw);
                            break;
                        case FastGameParamValueType.Int:
                            write.IntValue = Convert.ToInt32(raw);
                            break;
                        case FastGameParamValueType.Float:
                            write.FloatValue = Convert.ToSingle(raw);
                            break;
                        case FastGameParamValueType.String:
                            write.StringValue = raw.ToString();
                            break;
                    }
                }
                list.Add(write);
            }
            return list.ToArray();
        }

        static FastGameParamChannel ParseChannel(string raw)
        {
            switch ((raw ?? "animator").Trim().ToLowerInvariant())
            {
                case "material":
                    return FastGameParamChannel.Material;
                case "component":
                    return FastGameParamChannel.Component;
                case "flow_var":
                case "flowvar":
                    return FastGameParamChannel.FlowVar;
                default:
                    return FastGameParamChannel.Animator;
            }
        }

        static FastGameParamValueType ParseType(string raw)
        {
            switch ((raw ?? "float").Trim().ToLowerInvariant())
            {
                case "bool":
                    return FastGameParamValueType.Bool;
                case "int":
                    return FastGameParamValueType.Int;
                case "string":
                    return FastGameParamValueType.String;
                case "trigger":
                    return FastGameParamValueType.Trigger;
                default:
                    return FastGameParamValueType.Float;
            }
        }
    }
}
