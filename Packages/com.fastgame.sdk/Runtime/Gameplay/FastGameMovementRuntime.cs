using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Tip movement_profile → simple CharacterController locomotion (G1 fill).
    /// Profiles: humanoid (default), fly. Vehicle profiles reserved for G4.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Movement Runtime")]
    public sealed class FastGameMovementRuntime : MonoBehaviour
    {
        public CharacterController Controller;
        public string MovementProfile = "humanoid";
        public float WalkSpeed = 4.5f;
        public float SprintMultiplier = 1.6f;
        public float Gravity = -18f;
        public bool EnablePlayerInput = true;

        [Tooltip("Animator Speed float — optional binding to move magnitude.")]
        public FastGameParamRuntime Params;
        public string SpeedParamName = "Speed";

        Vector3 _velocity;
        bool _sprint;

        void Awake()
        {
            if (Controller == null)
                Controller = GetComponent<CharacterController>()
                    ?? GetComponentInChildren<CharacterController>(true);
            if (Params == null)
                Params = GetComponent<FastGameParamRuntime>();
        }

        public void ApplyMovementProfile(string profile)
        {
            MovementProfile = string.IsNullOrWhiteSpace(profile) ? "humanoid" : profile.Trim();
        }

        public void SetSprint(bool sprint) => _sprint = sprint;

        void Update()
        {
            if (!EnablePlayerInput || Controller == null || !Controller.enabled)
                return;

            if (IsVehicleProfile())
                return;

            var x = Input.GetAxisRaw("Horizontal");
            var z = Input.GetAxisRaw("Vertical");
            _sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Fire3");

            var input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var speed = WalkSpeed * (_sprint ? SprintMultiplier : 1f);
            if (string.Equals(MovementProfile, "fly", System.StringComparison.OrdinalIgnoreCase))
            {
                var fly = transform.TransformDirection(input) * speed;
                if (Input.GetKey(KeyCode.E)) fly.y += WalkSpeed;
                if (Input.GetKey(KeyCode.Q)) fly.y -= WalkSpeed;
                Controller.Move(fly * Time.deltaTime);
            }
            else
            {
                var move = transform.TransformDirection(input) * speed;
                if (Controller.isGrounded && _velocity.y < 0f)
                    _velocity.y = -1f;
                _velocity.y += Gravity * Time.deltaTime;
                move.y = _velocity.y;
                Controller.Move(move * Time.deltaTime);
            }

            if (Params != null && !string.IsNullOrEmpty(SpeedParamName))
                Params.SetAnimator(SpeedParamName, input.magnitude * (_sprint ? SprintMultiplier : 1f));
        }

        bool IsVehicleProfile()
        {
            var p = MovementProfile ?? "";
            return p.StartsWith("vehicle_", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
