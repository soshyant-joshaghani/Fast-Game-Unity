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

    /// <summary>Tip-fed ability activate → ParamRuntime writes. No per-title C# rules.</summary>
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
    }
}
