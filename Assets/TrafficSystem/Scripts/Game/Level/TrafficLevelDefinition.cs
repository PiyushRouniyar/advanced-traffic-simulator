using System;
using System.Collections.Generic;
using MyTrafficSystem.Managers;
using MyTrafficSystem.Pedestrians;
using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Level
{
    [CreateAssetMenu(menuName = "Traffic Game/Level Definition", fileName = "TrafficLevelDefinition")]
    public class TrafficLevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelId = "LEVEL_01";
        [SerializeField] private string displayName = "Level 1 - Tutorial";
        [TextArea] [SerializeField] private string introText = "Manage traffic and keep intersections flowing.";

        [Header("Flow")]
        [SerializeField] private float levelDurationSeconds = 180f;
        [SerializeField] private float introDurationSeconds = 4f;

        [Header("Objectives")]
        [SerializeField] private float maxCongestionNormalized = 0.85f;
        [SerializeField] private float failCongestionDurationSeconds = 10f;
        [SerializeField] private int maxCriticalStalledVehicles = 35;
        [SerializeField] private bool requirePedestrianSafety = true;

        [Header("Pressure")]
        [SerializeField] private float startTrafficSpawnMultiplier = 1f;
        [SerializeField] private float endTrafficSpawnMultiplier = 2f;
        [SerializeField] private float startPedestrianSpawnMultiplier = 1f;
        [SerializeField] private float endPedestrianSpawnMultiplier = 1.8f;

        [Header("Scene References")]
        [SerializeField] private List<AutomaticTrafficSpawner> carSpawners = new List<AutomaticTrafficSpawner>();
        [SerializeField] private List<CitizenSpawner> citizenSpawners = new List<CitizenSpawner>();
        [SerializeField] private List<TrafficIntersectionManager> controlledIntersections = new List<TrafficIntersectionManager>();
        [SerializeField] private List<Transform> cctvCameraAnchors = new List<Transform>();

        public string LevelId => levelId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? levelId : displayName;
        public string IntroText => introText;
        public float LevelDurationSeconds => Mathf.Max(30f, levelDurationSeconds);
        public float IntroDurationSeconds => Mathf.Max(0f, introDurationSeconds);

        public float MaxCongestionNormalized => Mathf.Clamp01(maxCongestionNormalized);
        public float FailCongestionDurationSeconds => Mathf.Max(1f, failCongestionDurationSeconds);
        public int MaxCriticalStalledVehicles => Mathf.Max(1, maxCriticalStalledVehicles);
        public bool RequirePedestrianSafety => requirePedestrianSafety;

        public float StartTrafficSpawnMultiplier => Mathf.Max(0.1f, startTrafficSpawnMultiplier);
        public float EndTrafficSpawnMultiplier => Mathf.Max(0.1f, endTrafficSpawnMultiplier);
        public float StartPedestrianSpawnMultiplier => Mathf.Max(0.1f, startPedestrianSpawnMultiplier);
        public float EndPedestrianSpawnMultiplier => Mathf.Max(0.1f, endPedestrianSpawnMultiplier);

        public IReadOnlyList<AutomaticTrafficSpawner> CarSpawners => carSpawners;
        public IReadOnlyList<CitizenSpawner> CitizenSpawners => citizenSpawners;
        public IReadOnlyList<TrafficIntersectionManager> ControlledIntersections => controlledIntersections;
        public IReadOnlyList<Transform> CctvCameraAnchors => cctvCameraAnchors;

        public void AutoPopulateFromScene()
        {
            carSpawners = new List<AutomaticTrafficSpawner>(FindObjectsByType<AutomaticTrafficSpawner>(FindObjectsSortMode.None));
            citizenSpawners = new List<CitizenSpawner>(FindObjectsByType<CitizenSpawner>(FindObjectsSortMode.None));
            controlledIntersections = new List<TrafficIntersectionManager>(FindObjectsByType<TrafficIntersectionManager>(FindObjectsSortMode.None));
        }

        [Serializable]
        public struct ScoreThresholds
        {
            public int oneStar;
            public int twoStar;
            public int threeStar;
        }
    }
}
