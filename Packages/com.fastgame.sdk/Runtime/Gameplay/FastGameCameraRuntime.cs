using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Tip camera_profile → follow / top-down / fps (G1 fill).
    /// Profiles: tps_follow (default), top_down, fps, orbit, fixed.
    /// </summary>
    [AddComponentMenu("Fast Game/Gameplay/Camera Runtime")]
    public sealed class FastGameCameraRuntime : MonoBehaviour
    {
        public Camera RigCamera;
        public Transform FollowTarget;
        public string CameraProfile = "tps_follow";

        public Vector3 TpsOffset = new Vector3(0f, 2.2f, -4.5f);
        public Vector3 TopDownOffset = new Vector3(0f, 18f, -0.1f);
        public float FollowLerp = 12f;
        public float LookLerp = 10f;

        void Awake()
        {
            if (RigCamera == null)
                RigCamera = Camera.main;
        }

        public void ApplyCameraProfile(string profile)
        {
            CameraProfile = string.IsNullOrWhiteSpace(profile) ? "tps_follow" : profile.Trim();
        }

        public void SetFollowTarget(Transform target) => FollowTarget = target;

        void LateUpdate()
        {
            if (RigCamera == null || FollowTarget == null)
                return;

            var profile = (CameraProfile ?? "tps_follow").Trim().ToLowerInvariant();
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
                case "orbit":
                case "tps_follow":
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
