using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    public static class ConnectionValidator
    {
        public static bool IsValid(Lane from, Lane to, out string reason)
        {
            reason = string.Empty;
            if (from == null || to == null) { reason = "Source/target lane missing."; return false; }
            if (from == to) { reason = "Lane cannot connect to itself."; return false; }
            if (from.EndWaypoint == null || to.StartWaypoint == null) { reason = "Lane endpoints missing."; return false; }

            Vector3 fromDir = GetEndDirection(from);
            Vector3 toDir = GetStartDirection(to);
            if (fromDir.sqrMagnitude > 0.001f && toDir.sqrMagnitude > 0.001f)
            {
                float alignment = Vector3.Dot(fromDir.normalized, toDir.normalized);
                if (alignment < -0.35f) { reason = "Connection direction invalid."; return false; }
            }

            return true;
        }

        private static Vector3 GetEndDirection(Lane lane)
        {
            if (lane.Waypoints.Count < 2) { return Vector3.zero; }
            Vector3 a = lane.Waypoints[lane.Waypoints.Count - 2].transform.position;
            Vector3 b = lane.Waypoints[lane.Waypoints.Count - 1].transform.position;
            return b - a;
        }

        private static Vector3 GetStartDirection(Lane lane)
        {
            if (lane.Waypoints.Count < 2) { return Vector3.zero; }
            return lane.Waypoints[1].transform.position - lane.Waypoints[0].transform.position;
        }
    }
}
