using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianCrosswalkNode : MonoBehaviour
    {
        [SerializeField] private TrafficLightGroup linkedTrafficGroup;
        [SerializeField] private bool invertCarsGreen = true;
        [SerializeField] private bool drawDebug = true;

        public TrafficLightGroup LinkedTrafficGroup => linkedTrafficGroup;

        public bool CanPedestriansCross
        {
            get
            {
                if (linkedTrafficGroup == null) return true;
                bool carsGreen = linkedTrafficGroup.IsGreen;
                return invertCarsGreen ? !carsGreen : carsGreen;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawDebug) return;
            Gizmos.color = CanPedestriansCross ? new Color(0.45f, 1f, 0.62f, 1f) : new Color(1f, 0.4f, 0.4f, 1f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.16f);
        }
    }
}
