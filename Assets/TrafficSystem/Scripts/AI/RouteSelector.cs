using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    // Compatibility helper for older setups. New simple AI can ignore this.
    [DisallowMultipleComponent]
    public class RouteSelector : MonoBehaviour
    {
        [SerializeField] private bool randomRouteSelection = true;

        public Lane ChooseNextLane(Lane currentLane)
        {
            if (currentLane == null || currentLane.ConnectedLanes.Count == 0)
            {
                return null;
            }

            if (!randomRouteSelection)
            {
                return currentLane.ConnectedLanes[0];
            }

            int idx = Random.Range(0, currentLane.ConnectedLanes.Count);
            return currentLane.ConnectedLanes[idx];
        }

        public void SetRandomRouteSelection(bool value)
        {
            randomRouteSelection = value;
        }
    }
}
