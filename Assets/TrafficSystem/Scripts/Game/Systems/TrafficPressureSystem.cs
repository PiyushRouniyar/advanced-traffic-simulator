using System;
using MyTrafficSystem.Gameplay.Level;
using MyTrafficSystem.Managers;
using MyTrafficSystem.Pedestrians;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Systems
{
    [DisallowMultipleComponent]
    public class TrafficPressureSystem : MonoBehaviour
    {
        [SerializeField] private TrafficLevelDefinition activeLevel;

        public float Pressure01 { get; private set; }

        public event Action<float> PressureChanged;

        public void SetLevel(TrafficLevelDefinition level)
        {
            activeLevel = level;
        }

        public void Tick(float elapsedSeconds)
        {
            if (activeLevel == null) return;

            Pressure01 = Mathf.Clamp01(elapsedSeconds / Mathf.Max(1f, activeLevel.LevelDurationSeconds));

            float carMultiplier = Mathf.Lerp(activeLevel.StartTrafficSpawnMultiplier, activeLevel.EndTrafficSpawnMultiplier, Pressure01);
            float pedMultiplier = Mathf.Lerp(activeLevel.StartPedestrianSpawnMultiplier, activeLevel.EndPedestrianSpawnMultiplier, Pressure01);

            ApplyCarPressure(carMultiplier);
            ApplyCitizenPressure(pedMultiplier);
            PressureChanged?.Invoke(Pressure01);
        }

        private void ApplyCarPressure(float multiplier)
        {
            var spawners = activeLevel.CarSpawners;
            for (int i = 0; i < spawners.Count; i++)
            {
                AutomaticTrafficSpawner sp = spawners[i];
                if (sp == null) continue;

                sp.SetTrafficRunning(true);
                sp.SpawnInterval = Mathf.Max(0.25f, 1.2f / Mathf.Max(0.2f, multiplier));
                sp.MaxActiveCars = Mathf.RoundToInt(Mathf.Lerp(40f, 220f, Pressure01));
            }
        }

        private void ApplyCitizenPressure(float multiplier)
        {
            var spawners = activeLevel.CitizenSpawners;
            for (int i = 0; i < spawners.Count; i++)
            {
                CitizenSpawner sp = spawners[i];
                if (sp == null) continue;

                sp.StartSpawning();
            }
        }
    }
}
