using System;
using MyTrafficSystem.Gameplay.Level;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Systems
{
    [DisallowMultipleComponent]
    public class TrafficObjectiveSystem : MonoBehaviour
    {
        [SerializeField] private TrafficLevelDefinition activeLevel;
        [SerializeField] private TrafficCongestionMonitor congestionMonitor;

        private float overCongestionTimer;

        public event Action<string> ObjectiveFailed;

        public bool HasFailed { get; private set; }
        public string FailReason { get; private set; }

        public void Configure(TrafficLevelDefinition level, TrafficCongestionMonitor monitor)
        {
            activeLevel = level;
            congestionMonitor = monitor;
            overCongestionTimer = 0f;
            HasFailed = false;
            FailReason = string.Empty;
        }

        public void Tick(float deltaTime)
        {
            if (HasFailed || activeLevel == null || congestionMonitor == null) return;

            bool overCongestion = congestionMonitor.NormalizedCongestion >= activeLevel.MaxCongestionNormalized;
            overCongestionTimer = overCongestion ? overCongestionTimer + deltaTime : 0f;

            if (overCongestionTimer >= activeLevel.FailCongestionDurationSeconds)
            {
                TriggerFail("Severe congestion sustained too long.");
                return;
            }

            if (congestionMonitor.StalledVehicleCount >= activeLevel.MaxCriticalStalledVehicles)
            {
                TriggerFail("Intersection deadlock risk: too many stalled vehicles.");
                return;
            }

            if (activeLevel.RequirePedestrianSafety && congestionMonitor.WaitingCitizenCount > Mathf.Max(20, activeLevel.MaxCriticalStalledVehicles))
            {
                TriggerFail("Pedestrian safety pressure exceeded.");
            }
        }

        private void TriggerFail(string reason)
        {
            HasFailed = true;
            FailReason = reason;
            ObjectiveFailed?.Invoke(reason);
        }
    }
}
