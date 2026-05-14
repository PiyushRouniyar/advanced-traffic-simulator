using UnityEngine;

namespace MyTrafficSystem.Gameplay.CCTV
{
    [DisallowMultipleComponent]
    public class CCTVCameraPoint : MonoBehaviour
    {
        [SerializeField] private string cameraLabel = "CAM-01";
        [SerializeField] private float priority = 1f;
        [SerializeField] private float fieldOfView = 60f;

        public string CameraLabel => string.IsNullOrWhiteSpace(cameraLabel) ? gameObject.name : cameraLabel;
        public float Priority => priority;
        public float FieldOfView => Mathf.Clamp(fieldOfView, 25f, 95f);
    }
}
