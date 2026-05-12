using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenPathDecider : MonoBehaviour
    {
        [SerializeField] private bool avoidImmediateBacktracking = true;
        [SerializeField] private bool fallbackToAnyPedestrianLane = true;

        public PedestrianLane GetNextLane(PedestrianLane currentLane, PedestrianLane previousLane)
        {
            if (currentLane == null)
            {
                return null;
            }

            PedestrianLane nextLane = avoidImmediateBacktracking
                ? currentLane.GetRandomConnectedLaneExcluding(previousLane)
                : currentLane.GetRandomConnectedLane();

            if (nextLane != null)
            {
                return nextLane;
            }

            if (!fallbackToAnyPedestrianLane)
            {
                return null;
            }

            var lanes = PedestrianLane.RegisteredLanes;
            if (lanes == null || lanes.Count == 0)
            {
                return null;
            }

            // Lightweight fallback so citizens never get permanently stuck.
            for (int tries = 0; tries < 8; tries++)
            {
                PedestrianLane candidate = lanes[Random.Range(0, lanes.Count)];
                if (candidate != null && candidate.StartWaypoint != null)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
