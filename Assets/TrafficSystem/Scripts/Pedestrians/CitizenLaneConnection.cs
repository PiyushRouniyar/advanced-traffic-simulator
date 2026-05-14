using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenLaneConnection : MonoBehaviour
    {
        [SerializeField] private CitizenLane fromLane;
        [SerializeField] private CitizenLane toLane;

        public CitizenLane FromLane => fromLane;
        public CitizenLane ToLane => toLane;

        public bool TryAssign(CitizenLane from, CitizenLane to)
        {
            if (from == null || to == null || from == to) return false;

            if (fromLane != null) fromLane.UnregisterOutgoingConnection(this);

            fromLane = from;
            toLane = to;
            fromLane.RegisterOutgoingConnection(this);
            return true;
        }

        private void OnEnable()
        {
            if (fromLane != null) fromLane.RegisterOutgoingConnection(this);
        }

        private void OnDisable()
        {
            if (fromLane != null) fromLane.UnregisterOutgoingConnection(this);
        }

        private void OnDrawGizmos()
        {
            if (!CitizenDebugSettings.ShowDebug || !CitizenDebugSettings.ShowTrafficAssignments) return;
            if (fromLane == null || toLane == null || fromLane.EndWaypoint == null || toLane.StartWaypoint == null) return;

            Gizmos.color = new Color(1f, 0.93f, 0.3f, 1f);
            Vector3 a = fromLane.EndWaypoint.transform.position;
            Vector3 b = toLane.StartWaypoint.transform.position;
            Vector3 tanA = a + (b - a) * 0.33f + Vector3.up * 0.2f;
            Vector3 tanB = a + (b - a) * 0.66f + Vector3.up * 0.2f;

            Vector3 prev = a;
            for (int i = 1; i <= 14; i++)
            {
                float t = i / 14f;
                Vector3 p = Bezier(a, tanA, tanB, b, t);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }
    }
}
