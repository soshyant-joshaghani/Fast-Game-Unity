using System;
using UnityEngine;
using UnityEngine.Events;

namespace FastGame
{
    [Serializable]
    public class FastGameParamWriteEvent : UnityEvent<string, FastGameParamChannel> { }

    /// <summary>
    /// Applies tip / Flow SetAnimator · SetMaterial · SetVar writes onto this entity's art.
    /// Declared param NAMEs must exist on Animator / Material / component.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Param Runtime")]
    public sealed class FastGameParamRuntime : MonoBehaviour
    {
        [Tooltip("Animator used for channel=Animator (defaults to child Animator).")]
        public Animator TargetAnimator;

        [Tooltip("Renderer for MaterialPropertyBlock floats/bools (defaults to child Renderer).")]
        public Renderer TargetRenderer;

        [Header("Events")]
        public FastGameParamWriteEvent OnParamWritten;
        public UnityEvent<string> OnParamFailed;

        MaterialPropertyBlock _mpb;

        void Awake()
        {
            if (TargetAnimator == null)
                TargetAnimator = GetComponentInChildren<Animator>(true);
            if (TargetRenderer == null)
                TargetRenderer = GetComponentInChildren<Renderer>(true);
        }

        public void SetAnimator(string paramName, float value) =>
            Apply(new FastGameParamWrite
            {
                Name = paramName,
                Channel = FastGameParamChannel.Animator,
                Type = FastGameParamValueType.Float,
                FloatValue = value
            });

        public void SetAnimator(string paramName, bool value) =>
            Apply(new FastGameParamWrite
            {
                Name = paramName,
                Channel = FastGameParamChannel.Animator,
                Type = FastGameParamValueType.Bool,
                BoolValue = value
            });

        public void SetAnimatorTrigger(string paramName) =>
            Apply(new FastGameParamWrite
            {
                Name = paramName,
                Channel = FastGameParamChannel.Animator,
                Type = FastGameParamValueType.Trigger
            });

        public void SetMaterial(string paramName, float value) =>
            Apply(new FastGameParamWrite
            {
                Name = paramName,
                Channel = FastGameParamChannel.Material,
                Type = FastGameParamValueType.Float,
                FloatValue = value
            });

        public void SetMaterial(string paramName, bool value) =>
            Apply(new FastGameParamWrite
            {
                Name = paramName,
                Channel = FastGameParamChannel.Material,
                Type = FastGameParamValueType.Float,
                FloatValue = value ? 1f : 0f
            });

        public bool Apply(FastGameParamWrite write)
        {
            var name = (write.Name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                OnParamFailed?.Invoke("empty param name");
                return false;
            }

            switch (write.Channel)
            {
                case FastGameParamChannel.Animator:
                    return ApplyAnimator(name, write);
                case FastGameParamChannel.Material:
                    return ApplyMaterial(name, write);
                case FastGameParamChannel.Component:
                case FastGameParamChannel.FlowVar:
                    // Host / Flow instance vars — reserved; Director may subscribe.
                    OnParamWritten?.Invoke(name, write.Channel);
                    return true;
                default:
                    OnParamFailed?.Invoke("unknown channel " + write.Channel);
                    return false;
            }
        }

        public void ApplyMany(FastGameParamWrite[] writes)
        {
            if (writes == null)
                return;
            for (var i = 0; i < writes.Length; i++)
                Apply(writes[i]);
        }

        bool ApplyAnimator(string name, FastGameParamWrite write)
        {
            if (TargetAnimator == null)
            {
                OnParamFailed?.Invoke("no Animator for " + name);
                return false;
            }

            switch (write.Type)
            {
                case FastGameParamValueType.Bool:
                    TargetAnimator.SetBool(name, write.BoolValue);
                    break;
                case FastGameParamValueType.Int:
                    TargetAnimator.SetInteger(name, write.IntValue);
                    break;
                case FastGameParamValueType.Float:
                    TargetAnimator.SetFloat(name, write.FloatValue);
                    break;
                case FastGameParamValueType.Trigger:
                    TargetAnimator.SetTrigger(name);
                    break;
                default:
                    OnParamFailed?.Invoke("unsupported animator type for " + name);
                    return false;
            }

            OnParamWritten?.Invoke(name, FastGameParamChannel.Animator);
            return true;
        }

        bool ApplyMaterial(string name, FastGameParamWrite write)
        {
            if (TargetRenderer == null)
            {
                OnParamFailed?.Invoke("no Renderer for " + name);
                return false;
            }

            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            TargetRenderer.GetPropertyBlock(_mpb);

            switch (write.Type)
            {
                case FastGameParamValueType.Float:
                case FastGameParamValueType.Bool:
                    _mpb.SetFloat(name, write.Type == FastGameParamValueType.Bool
                        ? (write.BoolValue ? 1f : 0f)
                        : write.FloatValue);
                    break;
                case FastGameParamValueType.Int:
                    _mpb.SetFloat(name, write.IntValue);
                    break;
                default:
                    OnParamFailed?.Invoke("unsupported material type for " + name);
                    return false;
            }

            TargetRenderer.SetPropertyBlock(_mpb);
            OnParamWritten?.Invoke(name, FastGameParamChannel.Material);
            return true;
        }
    }
}
