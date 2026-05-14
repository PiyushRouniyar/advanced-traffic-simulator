using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenLane : MonoBehaviour
    {
        private static readonly List<CitizenLane> AllLanes = new List<CitizenLane>();

        [Header("Lane")]
        [SerializeField] private string laneName = "CitizenLane";
        [SerializeField] private bool loop;
        [SerializeField] private List<CitizenWaypoint> waypoints = new List<CitizenWaypoint>();
        [SerializeField] private List<CitizenLane> connectedLanes = new List<CitizenLane>();
        [SerializeField] private List<CitizenLaneConnection> outgoingConnections = new List<CitizenLaneConnection>();

        [Header("Crossing Light Assignment")]
        [SerializeField] private MyTrafficSystem.TrafficLights.TrafficLightController assignedTrafficLight;
        [SerializeField] private int stopWaypointIndex = -1;

        [Header("Visual")]
        [SerializeField] private Color laneColor = new Color(0.15f, 0.95f, 0.72f, 1f);
        [SerializeField] private bool drawLane = true;
        [SerializeField] private bool drawArrows = true;
        [SerializeField] private bool smoothPath = true;
        [SerializeField] [Range(4, 24)] private int smoothSegmentsPerCurve = 10;

        public string LaneName { get => string.IsNullOrWhiteSpace(laneName) ? gameObject.name : laneName; set => laneName = value; }
        public bool Loop => loop;
        public Color LaneColor => laneColor;
        public int StopWaypointIndex => stopWaypointIndex;
        public MyTrafficSystem.TrafficLights.TrafficLightController AssignedTrafficLight => assignedTrafficLight;
        public IReadOnlyList<CitizenWaypoint> Waypoints => waypoints;
        public IReadOnlyList<CitizenLane> ConnectedLanes => connectedLanes;
        public CitizenWaypoint StartWaypoint => waypoints.Count > 0 ? waypoints[0] : null;
        public CitizenWaypoint EndWaypoint => waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : null;
        public static IReadOnlyList<CitizenLane> RegisteredLanes => AllLanes;

        public void RenameLane(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            laneName = newName.Trim();
            gameObject.name = laneName.Replace(" ", "_");
        }

        public void SetLoop(bool shouldLoop) => loop = shouldLoop;

        public void AssignTrafficLight(MyTrafficSystem.TrafficLights.TrafficLightController trafficLight, int stopIndex)
        {
            assignedTrafficLight = trafficLight;
            int maxIndex = Mathf.Max(-1, waypoints.Count - 1);
            // If not explicitly set, default to the crossing edge (lane end) so light logic always applies.
            stopWaypointIndex = stopIndex < 0 ? maxIndex : Mathf.Clamp(stopIndex, -1, maxIndex);
        }

        public bool ShouldStopAtAssignedLight(int currentWaypointIndex)
        {
            if (assignedTrafficLight == null) return false;

            int effectiveStopIndex = stopWaypointIndex;
            if (effectiveStopIndex < 0)
            {
                // Safety fallback: any lane with an assigned light gates at its end.
                effectiveStopIndex = Mathf.Max(0, waypoints.Count - 1);
            }

            if (effectiveStopIndex < 0 || waypoints.Count == 0) return false;

            bool canCross = assignedTrafficLight.CurrentState == MyTrafficSystem.TrafficLights.TrafficLightState.Red;
            if (canCross) return false;
            return currentWaypointIndex >= Mathf.Max(0, effectiveStopIndex - 1);
        }

        public void RefreshWaypointsFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                CitizenWaypoint wp = transform.GetChild(i).GetComponent<CitizenWaypoint>();
                if (wp != null) waypoints.Add(wp);
            }
            Cleanup();
        }

        public void AddWaypoint(CitizenWaypoint waypoint)
        {
            if (waypoint == null) return;
            Cleanup();
            if (!waypoints.Contains(waypoint)) waypoints.Add(waypoint);
        }

        public void InsertWaypointAt(int index, CitizenWaypoint waypoint)
        {
            if (waypoint == null) return;
            Cleanup();
            waypoints.Insert(Mathf.Clamp(index, 0, waypoints.Count), waypoint);
        }

        public void ConnectTo(CitizenLane target)
        {
            Cleanup();
            if (target == null || target == this || connectedLanes.Contains(target)) return;
            connectedLanes.Add(target);
        }

        public void RemoveConnectionTo(CitizenLane target)
        {
            if (target == null) return;
            connectedLanes.Remove(target);
            for (int i = outgoingConnections.Count - 1; i >= 0; i--)
            {
                CitizenLaneConnection connection = outgoingConnections[i];
                if (connection != null && connection.ToLane == target) outgoingConnections.RemoveAt(i);
            }
        }

        public void RegisterOutgoingConnection(CitizenLaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) return;
            if (!outgoingConnections.Contains(connection)) outgoingConnections.Add(connection);
            ConnectTo(connection.ToLane);
        }

        public void UnregisterOutgoingConnection(CitizenLaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) return;
            outgoingConnections.Remove(connection);
            connectedLanes.Remove(connection.ToLane);
        }

        public bool TryGetNextWaypoint(int currentIndex, out CitizenWaypoint next)
        {
            next = null;
            Cleanup();
            if (waypoints.Count == 0) return false;

            int nextIndex = currentIndex + 1;
            if (nextIndex >= waypoints.Count)
            {
                if (!loop) return false;
                nextIndex = 0;
            }

            next = waypoints[nextIndex];
            return next != null;
        }

        public CitizenLane GetRandomConnectedLane()
        {
            Cleanup();
            if (connectedLanes.Count == 0) return null;
            return connectedLanes[Random.Range(0, connectedLanes.Count)];
        }

        private void OnEnable()
        {
            if (!AllLanes.Contains(this)) AllLanes.Add(this);
        }

        private void OnDisable() => AllLanes.Remove(this);
        private void OnValidate() => Cleanup();
        private void OnTransformChildrenChanged() => RefreshWaypointsFromChildren();

        private void Cleanup()
        {
            if (waypoints == null) waypoints = new List<CitizenWaypoint>();
            if (connectedLanes == null) connectedLanes = new List<CitizenLane>();
            if (outgoingConnections == null) outgoingConnections = new List<CitizenLaneConnection>();

            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                if (waypoints[i] == null) waypoints.RemoveAt(i);
            }

            for (int i = connectedLanes.Count - 1; i >= 0; i--)
            {
                if (connectedLanes[i] == null || connectedLanes[i] == this) connectedLanes.RemoveAt(i);
            }

            for (int i = outgoingConnections.Count - 1; i >= 0; i--)
            {
                CitizenLaneConnection c = outgoingConnections[i];
                if (c == null || c.FromLane != this || c.ToLane == null) outgoingConnections.RemoveAt(i);
            }

            stopWaypointIndex = Mathf.Clamp(stopWaypointIndex, -1, Mathf.Max(-1, waypoints.Count - 1));
        }

        private void OnDrawGizmos()
        {
            if (!CitizenDebugSettings.ShowDebug || !drawLane || waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = laneColor;
            if (smoothPath && waypoints.Count > 2)
            {
                DrawSmoothPath();
            }
            else
            {
                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    DrawSegment(waypoints[i], waypoints[i + 1]);
                }
            }

            if (loop && waypoints.Count > 1)
            {
                DrawSegment(waypoints[waypoints.Count - 1], waypoints[0]);
            }
        }

        private void DrawSegment(CitizenWaypoint aWp, CitizenWaypoint bWp)
        {
            if (aWp == null || bWp == null) return;
            Vector3 a = aWp.transform.position;
            Vector3 b = bWp.transform.position;
            Gizmos.DrawLine(a, b);
            if (drawArrows) DrawArrow(a, b);
        }

        private void DrawSmoothPath()
        {
            int count = waypoints.Count;
            for (int i = 0; i < count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) continue;

                Vector3 p0 = GetWaypointPosition(Mathf.Max(0, i - 1));
                Vector3 p1 = GetWaypointPosition(i);
                Vector3 p2 = GetWaypointPosition(i + 1);
                Vector3 p3 = GetWaypointPosition(Mathf.Min(count - 1, i + 2));

                Vector3 prev = p1;
                for (int s = 1; s <= smoothSegmentsPerCurve; s++)
                {
                    float t = s / (float)smoothSegmentsPerCurve;
                    Vector3 p = CatmullRom(p0, p1, p2, p3, t);
                    Gizmos.DrawLine(prev, p);
                    if (drawArrows && s == smoothSegmentsPerCurve / 2) DrawArrow(prev, p);
                    prev = p;
                }
            }
        }

        private Vector3 GetWaypointPosition(int index)
        {
            CitizenWaypoint wp = waypoints[Mathf.Clamp(index, 0, waypoints.Count - 1)];
            return wp != null ? wp.transform.position : transform.position;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * ((2f * p1)
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private static void DrawArrow(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            Vector3 mid = Vector3.Lerp(from, to, 0.5f);
            Vector3 left = Quaternion.Euler(0f, 155f, 0f) * dir;
            Vector3 right = Quaternion.Euler(0f, -155f, 0f) * dir;
            Gizmos.DrawLine(mid, mid + left * 0.35f);
            Gizmos.DrawLine(mid, mid + right * 0.35f);
        }
    }
}
