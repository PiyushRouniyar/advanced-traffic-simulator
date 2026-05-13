using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    [DisallowMultipleComponent]
    public class TrafficRouteDecider : MonoBehaviour
    {
        [SerializeField] private bool preferNonDeadEndRoutes = true;
        [SerializeField] private bool useConnectionWeights = true;

        public Lane DecideNextLane(Lane currentLane)
        {
            if (currentLane == null)
            {
                return null;
            }

            if (useConnectionWeights)
            {
                Lane weighted = currentLane.GetWeightedConnectedLane(preferNonDeadEndRoutes);
                if (weighted != null)
                {
                    return weighted;
                }
            }

            return currentLane.GetRandomConnectedLane();
        }
    }
}
