using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    [DisallowMultipleComponent]
    public class Lane : MonoBehaviour
    {
        [Header("Lane")]
        [SerializeField] private string laneName = "Lane";
        [SerializeField] private float speed = 10f;
        [SerializeField] private bool loop;

        [Header("Traffic Light")]
        [SerializeField] private MyTrafficSystem.TrafficLights.TrafficLightController trafficLight;
        [SerializeField] private int stopWaypointIndex = -1;
        [SerializeField] private bool canCarsPass = true;

        [Header("Visual")]
        [SerializeField] private Color laneColor = new Color(0.15f, 0.7f, 1f, 1f);
        [SerializeField] private bool drawLane = true;

        [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
        [SerializeField] private List<Lane> connectedLanes = new List<Lane>();

        public string LaneName { get => laneName; set => laneName = value; }
        public float Speed => Mathf.Max(1f, speed);
        public float SpeedLimit => Speed;
        public bool Loop => loop;
        public MyTrafficSystem.TrafficLights.TrafficLightController TrafficLight => trafficLight;
        public int StopWaypointIndex => stopWaypointIndex;
        public bool CanCarsPass => canCarsPass;
        public IReadOnlyList<Waypoint> Waypoints => waypoints;
        public IReadOnlyList<Lane> ConnectedLanes => connectedLanes;

        public Waypoint StartWaypoint { get { CleanupDestroyedReferences(); return waypoints.Count > 0 ? waypoints[0] : null; } }
        public Waypoint EndWaypoint { get { CleanupDestroyedReferences(); return waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : null; } }

        public void AddWaypoint(Waypoint waypoint)
        {
            if (waypoint == null) { return; }
            CleanupDestroyedReferences();
            waypoint.SetOwner(this, waypoints.Count);
            waypoints.Add(waypoint);
        }

        public void RefreshWaypointsFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Waypoint wp = transform.GetChild(i).GetComponent<Waypoint>();
                if (wp == null) { continue; }
                wp.SetOwner(this, waypoints.Count);
                waypoints.Add(wp);
            }

            CleanupDestroyedReferences();
        }

        public void ConnectTo(Lane target)
        {
            CleanupDestroyedReferences();
            if (target == null || target == this || connectedLanes.Contains(target)) { return; }
            connectedLanes.Add(target);
        }

        public Lane GetRandomConnectedLane()
        {
            CleanupDestroyedReferences();
            if (connectedLanes.Count == 0) { return null; }
            return connectedLanes[Random.Range(0, connectedLanes.Count)];
        }

        public bool TryGetNextWaypoint(int currentIndex, out Waypoint nextWaypoint)
        {
            CleanupDestroyedReferences();
            nextWaypoint = null;
            if (waypoints.Count == 0) { return false; }

            int nextIndex = currentIndex + 1;
            if (nextIndex >= waypoints.Count)
            {
                if (!loop) { return false; }
                nextIndex = 0;
            }

            nextWaypoint = waypoints[nextIndex];
            return nextWaypoint != null;
        }

        public void RegisterOutgoingConnection(LaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) { return; }
            ConnectTo(connection.ToLane);
        }

        public void UnregisterOutgoingConnection(LaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) { return; }
            connectedLanes.Remove(connection.ToLane);
        }

        public bool ShouldStopAtLight(int currentWaypointIndex)
        {
            if (stopWaypointIndex < 0) { return false; }

            bool blockedByLaneState = !canCarsPass;
            bool blockedByLight = trafficLight != null && trafficLight.ShouldStopCars;
            if (!blockedByLaneState && !blockedByLight) { return false; }

            return currentWaypointIndex >= Mathf.Max(0, stopWaypointIndex - 1);
        }

        public void SetTrafficLight(MyTrafficSystem.TrafficLights.TrafficLightController light, int stopIndex)
        {
            trafficLight = light;
            stopWaypointIndex = Mathf.Clamp(stopIndex, -1, Mathf.Max(-1, waypoints.Count - 1));
        }

        public void SetCanCarsPass(bool value)
        {
            canCarsPass = value;
        }

        // Compatibility method retained for older scripts.
        public void SetTrafficLightGroup(MyTrafficSystem.TrafficLights.TrafficLightGroup group, bool isNorthSouthFlow)
        {
        }

        private void OnValidate()
        {
            speed = Mathf.Max(1f, speed);
            CleanupDestroyedReferences();
        }

        private void OnTransformChildrenChanged()
        {
            RefreshWaypointsFromChildren();
        }

        private void OnDrawGizmos()
        {
            if (!TrafficDebugSettings.ShowTrafficDebug) { return; }
            CleanupDestroyedReferences();
            if (!drawLane || waypoints.Count < 2) { return; }

            Gizmos.color = laneColor;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) { continue; }
                Vector3 a = waypoints[i].transform.position;
                Vector3 b = waypoints[i + 1].transform.position;
                Gizmos.DrawLine(a, b);
                DrawArrow(a, b);
            }

            if (loop && waypoints.Count > 1 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
            {
                Vector3 a = waypoints[waypoints.Count - 1].transform.position;
                Vector3 b = waypoints[0].transform.position;
                Gizmos.DrawLine(a, b);
                DrawArrow(a, b);
            }

            DrawConnectionGizmos();
        }

        private void DrawConnectionGizmos()
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < connectedLanes.Count; i++)
            {
                Lane target = connectedLanes[i];
                if (target == null || EndWaypoint == null || target.StartWaypoint == null) { continue; }

                Vector3 p0 = EndWaypoint.transform.position;
                Vector3 p3 = target.StartWaypoint.transform.position;
                Vector3 p1 = p0 + (p3 - p0) * 0.33f + Vector3.up * 0.2f;
                Vector3 p2 = p0 + (p3 - p0) * 0.66f + Vector3.up * 0.2f;

                Vector3 prev = p0;
                for (int s = 1; s <= 14; s++)
                {
                    float t = s / 14f;
                    Vector3 p = Bezier(p0, p1, p2, p3, t);
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
            }
        }

        private static void DrawArrow(Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from).normalized;
            if (dir.sqrMagnitude < 0.0001f) { return; }

            Vector3 mid = Vector3.Lerp(from, to, 0.5f);
            Gizmos.color = Color.cyan;
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, -160f, 0f) * Vector3.forward;
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 160f, 0f) * Vector3.forward;
            Gizmos.DrawLine(mid, mid + left * 0.5f);
            Gizmos.DrawLine(mid, mid + right * 0.5f);
            Gizmos.color = new Color(0.15f, 0.7f, 1f, 1f);
        }

        private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
        }

        private void CleanupDestroyedReferences()
        {
            if (waypoints == null) { waypoints = new List<Waypoint>(); }
            if (connectedLanes == null) { connectedLanes = new List<Lane>(); }

            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                if (waypoints[i] == null) { waypoints.RemoveAt(i); }
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null && (waypoints[i].Owner != this || waypoints[i].Index != i))
                {
                    waypoints[i].SetOwner(this, i);
                }
            }

            for (int i = connectedLanes.Count - 1; i >= 0; i--)
            {
                if (connectedLanes[i] == null || connectedLanes[i] == this) { connectedLanes.RemoveAt(i); }
            }

            if (stopWaypointIndex >= waypoints.Count) { stopWaypointIndex = waypoints.Count - 1; }
        }
    }
}
