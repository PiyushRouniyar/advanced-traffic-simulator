using System.Collections.Generic;
using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    public class TrafficLightGroup : MonoBehaviour
    {
        [SerializeField] private List<MyTrafficSystem.TrafficLights.TrafficLightController> lights = new List<MyTrafficSystem.TrafficLights.TrafficLightController>();
        [SerializeField] private List<Lane> controlledLanes = new List<Lane>();
        [SerializeField] private int stopWaypointIndex = 0;

        public void SetState(TrafficLightState state)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                lights[i].ForceState(state);
            }
        }

        public void AssignToLanes()
        {
            MyTrafficSystem.TrafficLights.TrafficLightController first = lights.Count > 0 ? lights[0] : null;
            for (int i = 0; i < controlledLanes.Count; i++)
            {
                if (controlledLanes[i] == null)
                {
                    continue;
                }

                controlledLanes[i].SetTrafficLight(first, stopWaypointIndex);
            }
        }

        public void AssignStateToLanes()
        {
            AssignToLanes();
        }
    }
}
