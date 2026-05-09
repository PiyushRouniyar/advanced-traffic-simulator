using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Waypoints
{
    /// <summary>
    /// Holds a list of waypoints and gives helper methods for path navigation.
    /// </summary>
    public class WaypointPath : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private bool loopPath = true;
        [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();

        [Header("Gizmos")]
        [SerializeField] private bool drawPathLines = true;
        [SerializeField] private Color pathLineColor = Color.yellow;

        public bool LoopPath => loopPath;
        public int WaypointCount => waypoints.Count;
        public IReadOnlyList<Waypoint> Waypoints => waypoints;

        public Waypoint GetWaypoint(int index)
        {
            if (waypoints.Count == 0)
            {
                return null;
            }

            if (index < 0 || index >= waypoints.Count)
            {
                return null;
            }

            return waypoints[index];
        }

        public int IndexOfWaypoint(Waypoint waypoint)
        {
            if (waypoint == null || waypoints.Count == 0)
            {
                return -1;
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == waypoint)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetNextIndex(int currentIndex)
        {
            if (waypoints.Count == 0)
            {
                return -1;
            }

            int nextIndex = currentIndex + 1;
            if (nextIndex < waypoints.Count)
            {
                return nextIndex;
            }

            return loopPath ? 0 : -1;
        }

        public int GetClosestWaypointIndex(Vector3 worldPosition)
        {
            if (waypoints.Count == 0)
            {
                return -1;
            }

            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < waypoints.Count; i++)
            {
                Waypoint waypoint = waypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                float distance = (waypoint.Position - worldPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        [ContextMenu("Auto Fill From Children")]
        private void AutoFillFromChildren()
        {
            waypoints.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Waypoint point = transform.GetChild(i).GetComponent<Waypoint>();
                if (point != null)
                {
                    waypoints.Add(point);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawPathLines || waypoints.Count < 2)
            {
                return;
            }

            Gizmos.color = pathLineColor;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                {
                    continue;
                }

                Gizmos.DrawLine(waypoints[i].Position, waypoints[i + 1].Position);
            }

            if (!loopPath || waypoints[waypoints.Count - 1] == null || waypoints[0] == null)
            {
                return;
            }

            Gizmos.DrawLine(waypoints[waypoints.Count - 1].Position, waypoints[0].Position);
        }
    }
}
