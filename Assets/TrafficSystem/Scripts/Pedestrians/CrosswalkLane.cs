using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CrosswalkLane : MonoBehaviour
    {
        [SerializeField] private PedestrianLane lane;
        [SerializeField] private PedestrianCrossingZone crossingZone;
        [SerializeField] private PedestrianCrossingController crossingController;

        public PedestrianLane Lane => lane;
        public PedestrianCrossingZone CrossingZone => crossingZone;
        public PedestrianCrossingController CrossingController => crossingController;

        private void OnValidate()
        {
            if (lane == null)
            {
                lane = GetComponent<PedestrianLane>();
            }

            if (crossingZone == null)
            {
                crossingZone = GetComponent<PedestrianCrossingZone>();
            }

            if (crossingController == null)
            {
                crossingController = GetComponent<PedestrianCrossingController>();
                if (crossingController == null)
                {
                    crossingController = GetComponent<CrosswalkController>();
                }
            }
        }
    }
}
