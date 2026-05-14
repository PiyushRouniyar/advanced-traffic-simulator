using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenWaypoint : MonoBehaviour
    {
        [SerializeField] private bool waitAtWaypoint;
        [SerializeField] private float minWaitTime;
        [SerializeField] private float maxWaitTime = 0.9f;
        [SerializeField] private CitizenCrossingNode crossingNode;

        public bool WaitAtWaypoint => waitAtWaypoint;
        public float MinWaitTime => Mathf.Max(0f, minWaitTime);
        public float MaxWaitTime => Mathf.Max(MinWaitTime, maxWaitTime);
        public CitizenCrossingNode CrossingNode => crossingNode;
        public bool RequiresCrossingCheck => crossingNode != null;

        private void OnDrawGizmos()
        {
            if (!CitizenDebugSettings.ShowDebug || !CitizenDebugSettings.ShowWaypointNodes) return;
            Gizmos.color = RequiresCrossingCheck ? new Color(1f, 0.75f, 0.2f, 1f) : new Color(0.2f, 0.95f, 0.9f, 1f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.08f, 0.11f);
        }
    }
}
