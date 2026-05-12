using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianCrossingController : MonoBehaviour
    {
        [SerializeField] private TrafficLightGroup linkedTrafficGroup;
        [SerializeField] private bool invertGroupState = true;

        public bool CanPedestriansCross
        {
            get
            {
                if (linkedTrafficGroup == null)
                {
                    return true;
                }

                bool carsGreen = linkedTrafficGroup.IsGreen;
                return invertGroupState ? !carsGreen : carsGreen;
            }
        }
    }
}
