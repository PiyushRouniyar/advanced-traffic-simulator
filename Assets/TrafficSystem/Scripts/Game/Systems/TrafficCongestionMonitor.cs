using System;
using MyTrafficSystem.AI;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Systems
{
    [DisallowMultipleComponent]
    public class TrafficCongestionMonitor : MonoBehaviour
    {
        [SerializeField] private float stalledVelocityThreshold = 0.35f;

        public int ActiveVehicleCount { get; private set; }
        public int StalledVehicleCount { get; private set; }
        public float NormalizedCongestion { get; private set; }
        public int WaitingCitizenCount { get; private set; }

        public event Action Updated;

        private void Update()
        {
            Evaluate();
            Updated?.Invoke();
        }

        public void Evaluate()
        {
            TrafficCarAI[] cars = FindObjectsByType<TrafficCarAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            ActiveVehicleCount = cars.Length;

            int stalled = 0;
            float totalWaitSignals = 0f;
            for (int i = 0; i < cars.Length; i++)
            {
                Rigidbody rb = cars[i] != null ? cars[i].GetComponent<Rigidbody>() : null;
                if (rb == null) continue;

                float speed = rb.linearVelocity.magnitude;
                if (speed <= stalledVelocityThreshold)
                {
                    stalled++;
                }

                if (cars[i].IsStoppedByAssignedLight)
                {
                    totalWaitSignals += 0.35f;
                }
            }

            StalledVehicleCount = stalled;
            NormalizedCongestion = ActiveVehicleCount <= 0
                ? 0f
                : Mathf.Clamp01((stalled + totalWaitSignals) / Mathf.Max(1f, ActiveVehicleCount));

            var citizens = FindObjectsByType<MyTrafficSystem.Pedestrians.CitizenAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int waiting = 0;
            for (int i = 0; i < citizens.Length; i++)
            {
                if (citizens[i] != null)
                {
                    var st = citizens[i].State;
                    if (st == MyTrafficSystem.Pedestrians.CitizenAI.CitizenState.Waiting ||
                        st == MyTrafficSystem.Pedestrians.CitizenAI.CitizenState.Blocked)
                    {
                        waiting++;
                    }
                }
            }
            WaitingCitizenCount = waiting;
        }
    }
}
