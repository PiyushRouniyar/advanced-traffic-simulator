using MyTrafficSystem.Gameplay.Level;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Systems
{
    [DisallowMultipleComponent]
    public class TrafficScoreSystem : MonoBehaviour
    {
        [SerializeField] private TrafficCongestionMonitor congestionMonitor;

        private float elapsed;
        private float cumulativeCongestion;
        private float cumulativeFlow;
        private float cumulativePedestrianPressure;

        public int CurrentScore { get; private set; }
        public float FlowEfficiency01 { get; private set; }
        public float PedestrianSafety01 { get; private set; }

        public void ResetForLevel(TrafficCongestionMonitor monitor)
        {
            congestionMonitor = monitor;
            elapsed = 0f;
            cumulativeCongestion = 0f;
            cumulativeFlow = 0f;
            cumulativePedestrianPressure = 0f;
            CurrentScore = 0;
            FlowEfficiency01 = 1f;
            PedestrianSafety01 = 1f;
        }

        public void Tick(float deltaTime)
        {
            if (congestionMonitor == null) return;

            elapsed += Mathf.Max(0f, deltaTime);
            float congestion = congestionMonitor.NormalizedCongestion;
            float flow = 1f - congestion;
            float ped = 1f - Mathf.Clamp01(congestionMonitor.WaitingCitizenCount / 30f);

            cumulativeCongestion += congestion * deltaTime;
            cumulativeFlow += flow * deltaTime;
            cumulativePedestrianPressure += ped * deltaTime;

            FlowEfficiency01 = elapsed <= 0f ? 0f : Mathf.Clamp01(cumulativeFlow / elapsed);
            PedestrianSafety01 = elapsed <= 0f ? 0f : Mathf.Clamp01(cumulativePedestrianPressure / elapsed);

            float throughputBonus = congestionMonitor.ActiveVehicleCount > 0
                ? Mathf.Clamp01((congestionMonitor.ActiveVehicleCount - congestionMonitor.StalledVehicleCount) / (float)congestionMonitor.ActiveVehicleCount)
                : 0f;

            CurrentScore = Mathf.RoundToInt(
                (FlowEfficiency01 * 550f)
                + (PedestrianSafety01 * 300f)
                + (throughputBonus * 150f));
        }

        public int CalculateStars(TrafficLevelDefinition level)
        {
            if (level == null) return 1;
            if (CurrentScore >= 850) return 3;
            if (CurrentScore >= 650) return 2;
            return 1;
        }
    }
}
