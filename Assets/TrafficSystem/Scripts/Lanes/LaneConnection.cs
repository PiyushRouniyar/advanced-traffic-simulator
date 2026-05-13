using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    [DisallowMultipleComponent]
    public class LaneConnection : MonoBehaviour
    {
        public enum ConnectionType { Straight, LeftTurn, RightTurn, Merge }

        [SerializeField] private Lane fromLane;
        [SerializeField] private Lane toLane;
        [SerializeField] private ConnectionType connectionType = ConnectionType.Straight;
        [SerializeField] private float turnPriority = 1f;
        [SerializeField] private Color connectionColor = new Color(1f, 0.92f, 0.2f, 1f);

        public Lane FromLane => fromLane;
        public Lane ToLane => toLane;
        public ConnectionType Type => connectionType;
        public float TurnPriority => turnPriority;

        public bool TryAssign(Lane from, Lane to, ConnectionType type = ConnectionType.Straight)
        {
            if (!ConnectionValidator.IsValid(from, to, out _)) { return false; }
            if (fromLane != null) { fromLane.UnregisterOutgoingConnection(this); }
            fromLane = from;
            toLane = to;
            connectionType = type;
            fromLane.RegisterOutgoingConnection(this);
            return true;
        }

        private void OnValidate()
        {
            turnPriority = Mathf.Max(0.1f, turnPriority);
            if (fromLane != null) { fromLane.RegisterOutgoingConnection(this); }
        }

        private void OnDestroy()
        {
            if (fromLane != null) { fromLane.UnregisterOutgoingConnection(this); }
        }

        private void OnDrawGizmos()
        {
            if (!TrafficDebugSettings.ShowDirectionArrows) { return; }
            if (fromLane == null || toLane == null || fromLane.EndWaypoint == null || toLane.StartWaypoint == null) { return; }
            Vector3 a = fromLane.EndWaypoint.transform.position;
            Vector3 b = toLane.StartWaypoint.transform.position;
            Vector3 tanA = a + fromLane.EndWaypoint.transform.forward * 2f;
            Vector3 tanB = b - toLane.StartWaypoint.transform.forward * 2f;

            Gizmos.color = connectionColor;
            Vector3 prev = a;
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f;
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
