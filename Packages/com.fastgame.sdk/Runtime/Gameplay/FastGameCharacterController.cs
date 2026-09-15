using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Owned humanoid locomotion (V2): move / jump N / stance + tip param binds.
    /// Profiles: humanoid (default), fly. Vehicle profiles reserved for V4.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Character Controller")]
    public sealed class FastGameCharacterController : MonoBehaviour
    {
        public CharacterController Controller;
        public string MovementProfile = "humanoid";
        public float WalkSpeed = 4.5f;
        public float SprintMultiplier = 1.6f;
        public float CrouchMultiplier = 0.45f;
        public float Gravity = -18f;
        public float JumpVelocity = 7f;
        public int JumpCount = 2;
        public bool EnablePlayerInput = true;

        [Tooltip("Animator Speed float — bound from tip ability.move binds.")]
        public FastGameParamRuntime Params;
        public string SpeedParamName = "Speed";
        public string CrouchParamName = "IsCrouching";
        public string SprintParamName = "IsSprinting";

        public FastGameAbilityRuntime Abilities;

        Vector3 _velocity;
        bool _sprint;
        bool _crouch;
        int _jumpsLeft;
        float _moveSpeedBind;

        public bool IsCrouching => _crouch;
        public bool IsSprinting => _sprint;
        public float MoveSpeedBind => _moveSpeedBind;

        void Awake()
        {
            if (Controller == null)
                Controller = GetComponent<CharacterController>()
                    ?? GetComponentInChildren<CharacterController>(true);
            if (Params == null)
                Params = GetComponent<FastGameParamRuntime>()
                    ?? GetComponentInChildren<FastGameParamRuntime>(true);
            if (Abilities == null)
                Abilities = GetComponent<FastGameAbilityRuntime>()
                    ?? GetComponentInChildren<FastGameAbilityRuntime>(true);
            _jumpsLeft = JumpCount;
        }

        public void ApplyMovementProfile(string profile)
        {
            MovementProfile = string.IsNullOrWhiteSpace(profile) ? "humanoid" : profile.Trim();
        }

        /// <summary>Apply walk/jump/stance numbers from character tip (top-level or stats).</summary>
        public void ApplyLocomotionFromTip(System.Collections.Generic.Dictionary<string, object> tip)
        {
            if (tip == null)
                return;
            var payload = FastGameJson.GetObject(tip, "payload") ?? tip;
            var stats = FastGameJson.GetObject(payload, "stats") ?? payload;

            var move = FastGameJson.GetString(payload, "movement_profile")
                ?? FastGameJson.GetString(stats, "movement_profile");
            if (!string.IsNullOrWhiteSpace(move))
                ApplyMovementProfile(move);

            if (TryFloat(payload, "walk_speed", out var walk) || TryFloat(stats, "walk_speed", out walk))
                WalkSpeed = walk;
            if (TryFloat(payload, "sprint_mult", out var sprint) || TryFloat(stats, "sprint_mult", out sprint))
                SprintMultiplier = sprint;
            if (TryFloat(payload, "crouch_mult", out var crouch) || TryFloat(stats, "crouch_mult", out crouch))
                CrouchMultiplier = crouch;
            if (TryFloat(payload, "gravity", out var grav) || TryFloat(stats, "gravity", out grav))
                Gravity = grav;
            if (TryFloat(payload, "jump_velocity", out var jv) || TryFloat(stats, "jump_velocity", out jv))
                JumpVelocity = jv;
            if (TryInt(payload, "jump_count", out var jc) || TryInt(stats, "jump_count", out jc))
            {
                JumpCount = Mathf.Max(1, jc);
                _jumpsLeft = JumpCount;
            }

            // Prefer jump ability tip overrides
            var abilities = FastGameJson.GetArray(payload, "abilities");
            if (abilities != null)
            {
                for (var i = 0; i < abilities.Count; i++)
                {
                    var ab = abilities[i] as System.Collections.Generic.Dictionary<string, object>;
                    if (ab == null)
                        continue;
                    var id = FastGameJson.GetString(ab, "ability_id") ?? "";
                    if (!string.Equals(id, "jump", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    var p = FastGameJson.GetObject(ab, "params");
                    if (p == null)
                        continue;
                    if (TryInt(p, "jump_count", out jc))
                    {
                        JumpCount = Mathf.Max(1, jc);
                        _jumpsLeft = JumpCount;
                    }
                    if (TryFloat(p, "jump_velocity", out jv))
                        JumpVelocity = jv;
                }
            }
        }

        public void SetSprint(bool sprint)
        {
            _sprint = sprint;
            if (Params != null && !string.IsNullOrEmpty(SprintParamName))
                Params.SetAnimator(SprintParamName, sprint);
        }

        public void SetCrouch(bool crouch)
        {
            _crouch = crouch;
            if (Params != null && !string.IsNullOrEmpty(CrouchParamName))
                Params.SetAnimator(CrouchParamName, crouch);
        }

        public void RequestJump()
        {
            if (_jumpsLeft <= 0)
                return;
            _velocity.y = JumpVelocity;
            _jumpsLeft--;
            Abilities?.ActivateAbility("jump");
        }

        void Update()
        {
            if (!EnablePlayerInput || Controller == null || !Controller.enabled)
                return;

            if (IsVehicleProfile())
                return;

            var x = Input.GetAxisRaw("Horizontal");
            var z = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            {
                SetCrouch(!_crouch);
                if (Abilities != null)
                {
                    if (_crouch)
                        Abilities.ActivateAbility("crouch");
                    else
                        Abilities.DeactivateAbility("crouch");
                }
            }

            var wantSprint = (Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Fire3")) && !_crouch;
            if (wantSprint != _sprint)
            {
                SetSprint(wantSprint);
                if (Abilities != null)
                {
                    if (_sprint)
                        Abilities.ActivateAbility("sprint");
                    else
                        Abilities.DeactivateAbility("sprint");
                }
            }

            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
                RequestJump();

            var input = new Vector3(x, 0f, z);
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var speed = WalkSpeed;
            if (_crouch)
                speed *= CrouchMultiplier;
            else if (_sprint)
                speed *= SprintMultiplier;

            if (string.Equals(MovementProfile, "fly", System.StringComparison.OrdinalIgnoreCase))
            {
                var fly = transform.TransformDirection(input) * speed;
                if (Input.GetKey(KeyCode.E))
                    fly.y += WalkSpeed;
                if (Input.GetKey(KeyCode.Q))
                    fly.y -= WalkSpeed;
                Controller.Move(fly * Time.deltaTime);
                _moveSpeedBind = input.magnitude * (speed / Mathf.Max(0.01f, WalkSpeed));
            }
            else
            {
                var move = transform.TransformDirection(input) * speed;
                if (Controller.isGrounded)
                {
                    if (_velocity.y < 0f)
                        _velocity.y = -1f;
                    _jumpsLeft = JumpCount;
                }
                _velocity.y += Gravity * Time.deltaTime;
                move.y = _velocity.y;
                Controller.Move(move * Time.deltaTime);
                _moveSpeedBind = input.magnitude * (speed / Mathf.Max(0.01f, WalkSpeed));
            }

            if (Params != null && !string.IsNullOrEmpty(SpeedParamName))
                Params.SetAnimator(SpeedParamName, _moveSpeedBind);
        }

        bool IsVehicleProfile()
        {
            var p = MovementProfile ?? "";
            return p.StartsWith("vehicle_", System.StringComparison.OrdinalIgnoreCase);
        }

        static bool TryFloat(System.Collections.Generic.Dictionary<string, object> d, string key, out float v)
        {
            v = 0f;
            if (d == null || !d.TryGetValue(key, out var raw) || raw == null)
                return false;
            try
            {
                v = System.Convert.ToSingle(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool TryInt(System.Collections.Generic.Dictionary<string, object> d, string key, out int v)
        {
            v = 0;
            if (d == null || !d.TryGetValue(key, out var raw) || raw == null)
                return false;
            try
            {
                v = System.Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
