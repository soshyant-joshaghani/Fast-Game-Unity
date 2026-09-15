using UnityEngine;

namespace FastGame
{
    /// <summary>Legacy G1 name — prefer <see cref="FastGameCameraController"/> (V2).</summary>
    [AddComponentMenu("Fast Game/Gameplay/Camera Runtime (legacy)")]
    public sealed class FastGameCameraRuntime : MonoBehaviour
    {
        public FastGameCameraController Controller;

        void Awake()
        {
            if (Controller == null)
                Controller = GetComponent<FastGameCameraController>()
                    ?? gameObject.AddComponent<FastGameCameraController>();
        }

        public Camera RigCamera
        {
            get => Controller != null ? Controller.RigCamera : null;
            set { if (Controller != null) Controller.RigCamera = value; }
        }
        public Transform FollowTarget
        {
            get => Controller != null ? Controller.FollowTarget : null;
            set { if (Controller != null) Controller.FollowTarget = value; }
        }
        public string CameraProfile
        {
            get => Controller != null ? Controller.CameraProfile : "tps";
            set { if (Controller != null) Controller.CameraProfile = value; }
        }
        public Vector3 TpsOffset
        {
            get => Controller != null ? Controller.TpsOffset : default;
            set { if (Controller != null) Controller.TpsOffset = value; }
        }
        public Vector3 TopDownOffset
        {
            get => Controller != null ? Controller.TopDownOffset : default;
            set { if (Controller != null) Controller.TopDownOffset = value; }
        }
        public float FollowLerp
        {
            get => Controller != null ? Controller.FollowLerp : 12f;
            set { if (Controller != null) Controller.FollowLerp = value; }
        }
        public float LookLerp
        {
            get => Controller != null ? Controller.LookLerp : 10f;
            set { if (Controller != null) Controller.LookLerp = value; }
        }

        public void ApplyCameraProfile(string profile) => Controller?.ApplyCameraProfile(profile);
        public void SetFollowTarget(Transform target) => Controller?.SetFollowTarget(target);
    }
}
