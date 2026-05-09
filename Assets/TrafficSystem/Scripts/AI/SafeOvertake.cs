using MyTrafficSystem.Waypoints;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    /// <summary>
    /// Contains lane change safety rules for overtaking.
    /// </summary>
    public class SafeOvertake : MonoBehaviour
    {
        [SerializeField] private LaneDetector laneDetector;

        public bool TryGetBestOvertakeLane(out WaypointPath targetLane)
        {
            targetLane = null;
            if (laneDetector == null)
            {
                laneDetector = GetComponent<LaneDetector>();
            }

            if (laneDetector == null)
            {
                return false;
            }

            if (laneDetector.IsLaneSafe(laneDetector.LeftLanePath))
            {
                targetLane = laneDetector.LeftLanePath;
                return true;
            }

            if (laneDetector.IsLaneSafe(laneDetector.RightLanePath))
            {
                targetLane = laneDetector.RightLanePath;
                return true;
            }

            return false;
        }

        public bool IsSafeToReturn(WaypointPath originalLane)
        {
            if (laneDetector == null)
            {
                laneDetector = GetComponent<LaneDetector>();
            }

            return laneDetector != null && laneDetector.IsLaneSafe(originalLane);
        }
    }
}
