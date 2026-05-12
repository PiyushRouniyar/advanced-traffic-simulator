using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianLaneConnection : MonoBehaviour
    {
        [SerializeField] private PedestrianLane fromLane;
        [SerializeField] private PedestrianLane toLane;

        public PedestrianLane FromLane => fromLane;
        public PedestrianLane ToLane => toLane;

        public bool TryAssign(PedestrianLane from, PedestrianLane to)
        {
            if (from == null || to == null || from == to)
            {
                return false;
            }

            if (fromLane != null)
            {
                fromLane.UnregisterOutgoingConnection(this);
            }

            fromLane = from;
            toLane = to;
            fromLane.RegisterOutgoingConnection(this);
            return true;
        }

        private void OnValidate()
        {
            if (fromLane != null)
            {
                fromLane.RegisterOutgoingConnection(this);
            }
        }

        private void OnDestroy()
        {
            if (fromLane != null)
            {
                fromLane.UnregisterOutgoingConnection(this);
            }
        }
    }
}
