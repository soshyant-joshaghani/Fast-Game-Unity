using System;
using UnityEngine;

namespace FastGame
{
    public enum FastGameParamChannel
    {
        Animator,
        Material,
        Component,
        FlowVar,
    }

    public enum FastGameParamValueType
    {
        Bool,
        Int,
        Float,
        String,
        Trigger,
    }

    /// <summary>Declared art/logic slot — names must match tip entity params[].</summary>
    [Serializable]
    public struct FastGameDeclaredParam
    {
        public string Name;
        public FastGameParamValueType Type;
        public FastGameParamChannel Channel;
        public float DefaultFloat;
        public bool DefaultBool;
        public int DefaultInt;
        public string DefaultString;
    }

    /// <summary>One write issued by Flow / ability (SetAnimator / SetMaterial).</summary>
    [Serializable]
    public struct FastGameParamWrite
    {
        public string Name;
        public FastGameParamChannel Channel;
        public FastGameParamValueType Type;
        public float FloatValue;
        public bool BoolValue;
        public int IntValue;
        public string StringValue;
    }
}
