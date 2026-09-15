using UnityEngine;

namespace FastGame
{
    /// <summary>Legacy G1 name — prefer <see cref="FastGameCharacterController"/> (V2).</summary>
    [AddComponentMenu("Fast Game/Gameplay/Movement Runtime (legacy)")]
    public sealed class FastGameMovementRuntime : MonoBehaviour
    {
        public FastGameCharacterController Controller;

        void Awake()
        {
            if (Controller == null)
                Controller = GetComponent<FastGameCharacterController>()
                    ?? gameObject.AddComponent<FastGameCharacterController>();
        }

        public CharacterController Character => Controller != null ? Controller.Controller : null;
        public string MovementProfile
        {
            get => Controller != null ? Controller.MovementProfile : "humanoid";
            set { if (Controller != null) Controller.MovementProfile = value; }
        }
        public float WalkSpeed
        {
            get => Controller != null ? Controller.WalkSpeed : 4.5f;
            set { if (Controller != null) Controller.WalkSpeed = value; }
        }
        public float SprintMultiplier
        {
            get => Controller != null ? Controller.SprintMultiplier : 1.6f;
            set { if (Controller != null) Controller.SprintMultiplier = value; }
        }
        public float Gravity
        {
            get => Controller != null ? Controller.Gravity : -18f;
            set { if (Controller != null) Controller.Gravity = value; }
        }
        public bool EnablePlayerInput
        {
            get => Controller != null && Controller.EnablePlayerInput;
            set { if (Controller != null) Controller.EnablePlayerInput = value; }
        }
        public FastGameParamRuntime Params
        {
            get => Controller != null ? Controller.Params : null;
            set { if (Controller != null) Controller.Params = value; }
        }
        public string SpeedParamName
        {
            get => Controller != null ? Controller.SpeedParamName : "Speed";
            set { if (Controller != null) Controller.SpeedParamName = value; }
        }

        public void ApplyMovementProfile(string profile) => Controller?.ApplyMovementProfile(profile);
        public void SetSprint(bool sprint) => Controller?.SetSprint(sprint);
    }
}
