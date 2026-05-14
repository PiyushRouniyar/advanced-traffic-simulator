using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianLane : MonoBehaviour
    {
        private static readonly List<PedestrianLane> AllLanes = new List<PedestrianLane>();
        [Header("Path")]
        [SerializeField] private string pathName = "PedestrianPath";
        [SerializeField] private bool loop;
        [SerializeField] private List<PedestrianWaypoint> waypoints = new List<PedestrianWaypoint>();
        [SerializeField] private List<PedestrianLane> connectedLanes = new List<PedestrianLane>();
        [SerializeField] private List<PedestrianLaneConnection> outgoingConnections = new List<PedestrianLaneConnection>();
        [SerializeField] private Color debugColor = new Color(0.25f, 0.95f, 0.72f, 1f);
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawArrows = true;

        public string PathName { get => string.IsNullOrWhiteSpace(pathName) ? gameObject.name : pathName; set => pathName = value; }
        public bool Loop => loop;
        public Color DebugColor => debugColor;
        public IReadOnlyList<PedestrianWaypoint> Waypoints => waypoints;
        public IReadOnlyList<PedestrianLane> ConnectedLanes => connectedLanes;
        public PedestrianWaypoint StartWaypoint => waypoints.Count > 0 ? waypoints[0] : null;
        public PedestrianWaypoint EndWaypoint => waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : null;
        public static IReadOnlyList<PedestrianLane> RegisteredLanes => AllLanes;

        public void RenamePath(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;
            pathName = newName.Trim();
            gameObject.name = pathName.Replace(" ", "_");
        }

        public void RefreshWaypointsFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                PedestrianWaypoint wp = transform.GetChild(i).GetComponent<PedestrianWaypoint>();
                if (wp == null) continue;
                waypoints.Add(wp);
            }
            Cleanup();
        }

        public void AddWaypoint(PedestrianWaypoint waypoint)
        {
            if (waypoint == null) return;
            Cleanup();
            if (!waypoints.Contains(waypoint)) waypoints.Add(waypoint);
        }

        public void InsertWaypointAt(int index, PedestrianWaypoint waypoint)
        {
            if (waypoint == null) return;
            Cleanup();
            int clamped = Mathf.Clamp(index, 0, waypoints.Count);
            waypoints.Insert(clamped, waypoint);
        }

        public void ConnectTo(PedestrianLane target)
        {
            Cleanup();
            if (target == null || target == this || connectedLanes.Contains(target)) return;
            connectedLanes.Add(target);
        }

        public void RemoveConnectionTo(PedestrianLane target)
        {
            if (target == null) return;
            connectedLanes.Remove(target);
            for (int i = outgoingConnections.Count - 1; i >= 0; i--)
            {
                PedestrianLaneConnection connection = outgoingConnections[i];
                if (connection != null && connection.ToLane == target)
                {
                    outgoingConnections.RemoveAt(i);
                }
            }
        }

        public void RegisterOutgoingConnection(PedestrianLaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) return;
            if (!outgoingConnections.Contains(connection)) outgoingConnections.Add(connection);
            ConnectTo(connection.ToLane);
        }

        public void UnregisterOutgoingConnection(PedestrianLaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) return;
            outgoingConnections.Remove(connection);
            connectedLanes.Remove(connection.ToLane);
        }

        public bool TryGetNextWaypoint(int currentIndex, out PedestrianWaypoint next)
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

        public PedestrianLane GetRandomConnectedLane()
        {
            Cleanup();
            if (connectedLanes.Count == 0) return null;
            return connectedLanes[Random.Range(0, connectedLanes.Count)];
        }

        public PedestrianLane GetRandomConnectedLaneExcluding(PedestrianLane excludedLane)
        {
            Cleanup();
            if (connectedLanes.Count == 0) return null;
            if (excludedLane == null) return GetRandomConnectedLane();

            List<PedestrianLane> candidates = new List<PedestrianLane>();
            for (int i = 0; i < connectedLanes.Count; i++)
            {
                if (connectedLanes[i] != excludedLane)
                {
                    candidates.Add(connectedLanes[i]);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            return GetRandomConnectedLane();
        }

        private void OnValidate() => Cleanup();
        private void OnTransformChildrenChanged() => RefreshWaypointsFromChildren();
        private void OnEnable()
        {
            if (!AllLanes.Contains(this)) AllLanes.Add(this);
        }

        private void OnDisable()
        {
            AllLanes.Remove(this);
        }

        private void Cleanup()
        {
            if (waypoints == null) waypoints = new List<PedestrianWaypoint>();
            if (connectedLanes == null) connectedLanes = new List<PedestrianLane>();
            if (outgoingConnections == null) outgoingConnections = new List<PedestrianLaneConnection>();

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
                PedestrianLaneConnection connection = outgoingConnections[i];
                if (connection == null || connection.FromLane != this || connection.ToLane == null)
                {
                    outgoingConnections.RemoveAt(i);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = debugColor;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) continue;
                Vector3 a = waypoints[i].transform.position;
                Vector3 b = waypoints[i + 1].transform.position;
                Gizmos.DrawLine(a, b);
                if (drawArrows) DrawArrow(a, b);
            }

            if (loop && waypoints.Count > 1 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
            {
                Vector3 a = waypoints[waypoints.Count - 1].transform.position;
                Vector3 b = waypoints[0].transform.position;
                Gizmos.DrawLine(a, b);
                if (drawArrows) DrawArrow(a, b);
            }
        }

        private static void DrawArrow(Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from);
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
