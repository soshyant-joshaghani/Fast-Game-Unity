using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Tip camera_profile → follow / top-down / fps / side (V2 CameraController).
    /// Aliases: tps ≡ tps_follow; top-down ≡ top_down.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Camera Controller")]
    public sealed class FastGameCameraController : MonoBehaviour
    {
        public Camera RigCamera;
        public Transform FollowTarget;
        public string CameraProfile = "tps";

        public Vector3 TpsOffset = new Vector3(0f, 2.2f, -4.5f);
        public Vector3 TopDownOffset = new Vector3(0f, 18f, -0.1f);
        public Vector3 SideOffset = new Vector3(0f, 1.6f, -8f);
        public float FollowLerp = 12f;
        public float LookLerp = 10f;

        /// <summary>Optional allow-list from tip cameras[] (empty = all).</summary>
        public string[] AllowedCameras = System.Array.Empty<string>();

        void Awake()
        {
            if (RigCamera == null)
                RigCamera = Camera.main;
        }

        public void ApplyCameraProfile(string profile)
        {
            var raw = string.IsNullOrWhiteSpace(profile) ? "tps" : profile.Trim();
            var normalized = NormalizeProfile(raw);
            if (AllowedCameras != null && AllowedCameras.Length > 0)
            {
                var ok = false;
                for (var i = 0; i < AllowedCameras.Length; i++)
                {
                    if (string.Equals(
                            NormalizeProfile(AllowedCameras[i]),
                            normalized,
                            System.StringComparison.OrdinalIgnoreCase))
                    {
                        ok = true;
                        break;
                    }
                }
                if (!ok)
                    return;
            }
            CameraProfile = normalized;
        }

        public void SetFollowTarget(Transform target) => FollowTarget = target;

        public void ApplyAllowedCamerasFromTip(System.Collections.Generic.Dictionary<string, object> tip)
        {
            if (tip == null)
                return;
            var payload = FastGameJson.GetObject(tip, "payload") ?? tip;
            var cams = FastGameJson.GetArray(payload, "cameras")
                ?? FastGameJson.GetArray(FastGameJson.GetObject(payload, "stats"), "cameras");
            if (cams == null || cams.Count == 0)
                return;
            var list = new System.Collections.Generic.List<string>();
            for (var i = 0; i < cams.Count; i++)
            {
                var s = cams[i]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
            }
            AllowedCameras = list.ToArray();
        }

        public static string NormalizeProfile(string profile)
        {
            var p = (profile ?? "tps").Trim().ToLowerInvariant().Replace('-', '_');
            return p switch
            {
                "tps_follow" => "tps",
                "third_person" => "tps",
                "first_person" => "fps",
                "topdown" => "top_down",
                "side_scroll" => "side",
                _ => p,
            };
        }

        void LateUpdate()
        {
            if (RigCamera == null || FollowTarget == null)
                return;

            var profile = NormalizeProfile(CameraProfile);
            if (profile == "fixed")
                return;

            Vector3 desired;
            switch (profile)
            {
                case "fps":
                    desired = FollowTarget.position + FollowTarget.TransformVector(new Vector3(0f, 1.6f, 0.1f));
                    RigCamera.transform.position = desired;
                    RigCamera.transform.rotation = Quaternion.Slerp(
                        RigCamera.transform.rotation,
                        FollowTarget.rotation,
                        1f - Mathf.Exp(-LookLerp * Time.deltaTime));
                    return;
                case "top_down":
                    desired = FollowTarget.position + TopDownOffset;
                    break;
                case "side":
                    desired = FollowTarget.position + FollowTarget.TransformDirection(SideOffset);
                    break;
                case "orbit":
                case "tps":
                default:
                    desired = FollowTarget.position + FollowTarget.TransformDirection(TpsOffset);
                    break;
            }

            RigCamera.transform.position = Vector3.Lerp(
                RigCamera.transform.position,
                desired,
                1f - Mathf.Exp(-FollowLerp * Time.deltaTime));

            var look = FollowTarget.position + Vector3.up * 1.4f;
            var rot = Quaternion.LookRotation(look - RigCamera.transform.position, Vector3.up);
            RigCamera.transform.rotation = Quaternion.Slerp(
                RigCamera.transform.rotation,
                rot,
                1f - Mathf.Exp(-LookLerp * Time.deltaTime));
        }
    }
}
