using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianLane : MonoBehaviour
    {
        private static readonly List<PedestrianLane> AllLanes = new List<PedestrianLane>();

        [SerializeField] private string laneName = "PedestrianLane";
        [SerializeField] private bool loop = true;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color laneColor = new Color(0.2f, 0.9f, 0.5f, 1f);
        [SerializeField] private List<PedestrianWaypoint> waypoints = new List<PedestrianWaypoint>();
        [SerializeField] private List<PedestrianLane> connectedLanes = new List<PedestrianLane>();

        public string LaneName => laneName;
        public bool Loop => loop;
        public IReadOnlyList<PedestrianWaypoint> Waypoints => waypoints;
        public IReadOnlyList<PedestrianLane> ConnectedLanes => connectedLanes;
        public PedestrianWaypoint StartWaypoint => waypoints.Count > 0 ? waypoints[0] : null;

        public static IReadOnlyList<PedestrianLane> RegisteredLanes => AllLanes;

        public void RefreshWaypointsFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                PedestrianWaypoint wp = transform.GetChild(i).GetComponent<PedestrianWaypoint>();
                if (wp != null)
                {
                    waypoints.Add(wp);
                }
            }
            Cleanup();
        }

        public void ConnectTo(PedestrianLane target)
        {
            Cleanup();
            if (target == null || target == this || connectedLanes.Contains(target))
            {
                return;
            }
            connectedLanes.Add(target);
        }

        public PedestrianLane GetRandomConnectedLane()
        {
            Cleanup();
            if (connectedLanes.Count == 0) { return null; }
            return connectedLanes[Random.Range(0, connectedLanes.Count)];
        }

        public PedestrianLane GetRandomConnectedLaneExcluding(PedestrianLane excluded)
        {
            Cleanup();
            List<PedestrianLane> valid = new List<PedestrianLane>();
            for (int i = 0; i < connectedLanes.Count; i++)
            {
                PedestrianLane lane = connectedLanes[i];
                if (lane != null && lane != excluded)
                {
                    valid.Add(lane);
                }
            }

            if (valid.Count == 0)
            {
                return null;
            }

            return valid[Random.Range(0, valid.Count)];
        }

        public bool TryGetNextWaypoint(int currentIndex, out PedestrianWaypoint nextWaypoint)
        {
            nextWaypoint = null;
            Cleanup();
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

        public void RegisterOutgoingConnection(PedestrianLaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) { return; }
            ConnectTo(connection.ToLane);
        }

        public void UnregisterOutgoingConnection(PedestrianLaneConnection connection)
        {
            if (connection == null || connection.ToLane == null) { return; }
            connectedLanes.Remove(connection.ToLane);
        }

        private void OnValidate()
        {
            Cleanup();
        }

        private void OnEnable()
        {
            if (!AllLanes.Contains(this))
            {
                AllLanes.Add(this);
            }
        }

        private void OnDisable()
        {
            AllLanes.Remove(this);
        }

        private void OnTransformChildrenChanged()
        {
            RefreshWaypointsFromChildren();
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Cleanup();
            if (waypoints.Count < 2)
            {
                return;
            }

            Gizmos.color = laneColor;
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null) { continue; }
                Gizmos.DrawLine(waypoints[i].transform.position, waypoints[i + 1].transform.position);
            }
        }

        private void Cleanup()
        {
            for (int i = waypoints.Count - 1; i >= 0; i--)
            {
                if (waypoints[i] == null) { waypoints.RemoveAt(i); }
            }

            for (int i = connectedLanes.Count - 1; i >= 0; i--)
            {
                if (connectedLanes[i] == null || connectedLanes[i] == this) { connectedLanes.RemoveAt(i); }
            }
        }
    }
}
