using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.CCTV
{
    [DisallowMultipleComponent]
    public class CCTVCameraPoint : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string cameraLabel = "CAM_01 Downtown";
        [SerializeField] private string cameraGroup = "Default";

        [Header("Monitored Targets")]
        [SerializeField] private TrafficIntersectionManager monitoredIntersection;
        [SerializeField] private TrafficLightGroup monitoredTrafficLightGroup;

        [Header("View")]
        [SerializeField] private float priority = 1f;
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private bool allowZoom = true;
        [SerializeField] private float zoomFieldOfView = 42f;
        [SerializeField] private float maxViewRange = 120f;
        [SerializeField] private bool drawFrustum = true;
        [SerializeField] private bool drawRange = false;

        public string CameraGroup => cameraGroup;
        public TrafficIntersectionManager MonitoredIntersection => monitoredIntersection;
        public TrafficLightGroup MonitoredTrafficLightGroup => monitoredTrafficLightGroup;

        public string CameraLabel => string.IsNullOrWhiteSpace(cameraLabel) ? gameObject.name : cameraLabel;
        public float Priority => priority;
        public float FieldOfView => Mathf.Clamp(fieldOfView, 25f, 95f);
        public bool AllowZoom => allowZoom;
        public float ZoomFieldOfView => Mathf.Clamp(zoomFieldOfView, 20f, FieldOfView);
        public float MaxViewRange => Mathf.Max(20f, maxViewRange);

        public string IntersectionLabel => monitoredIntersection != null ? monitoredIntersection.name : "No Intersection";
        public string TrafficGroupLabel => monitoredTrafficLightGroup != null ? monitoredTrafficLightGroup.GroupName : "No Group";

        private void OnDrawGizmos()
        {
            Color c = new Color(0.25f, 0.95f, 1f, 0.85f);
            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2.5f);

            if (drawFrustum)
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.DrawFrustum(Vector3.zero, FieldOfView, 8f, 0.5f, 1.77f);
                Gizmos.matrix = old;
            }

            if (drawRange)
            {
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, MaxViewRange);
            }
        }
    }
}
