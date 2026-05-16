using System;
using MyTrafficSystem.AI;
using MyTrafficSystem.Lanes;
using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.Challenge
{
    public enum TrafficIncidentType
    {
        Collision,
        RedLightViolation,
        BlockedIntersection,
        LaneCongestion,
        StoppedTraffic,
        TrafficJam
    }

    public readonly struct TrafficIncidentData
    {
        public readonly TrafficIncidentType Type;
        public readonly Vector3 WorldPosition;
        public readonly Lane Lane;
        public readonly Lane OtherLane;
        public readonly TrafficCarAI PrimaryVehicle;
        public readonly TrafficCarAI SecondaryVehicle;
        public readonly TrafficIntersectionManager Intersection;
        public readonly float Timestamp;

        public TrafficIncidentData(
            TrafficIncidentType type,
            Vector3 worldPosition,
            Lane lane,
            Lane otherLane,
            TrafficCarAI primaryVehicle,
            TrafficCarAI secondaryVehicle,
            TrafficIntersectionManager intersection)
        {
            Type = type;
            WorldPosition = worldPosition;
            Lane = lane;
            OtherLane = otherLane;
            PrimaryVehicle = primaryVehicle;
            SecondaryVehicle = secondaryVehicle;
            Intersection = intersection;
            Timestamp = Time.time;
        }
    }

    [DefaultExecutionOrder(-350)]
    [DisallowMultipleComponent]
    public class TrafficIncidentSystem : MonoBehaviour
    {
        public static TrafficIncidentSystem Instance { get; private set; }

        public static event Action<TrafficIncidentData> IncidentReported;

        public static int GlobalIncidentCount { get; private set; }
        public static int GlobalCollisionCount { get; private set; }
        public static int GlobalRedLightViolationCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<TrafficIncidentSystem>(FindObjectsInactive.Include) != null) return;
            GameObject go = new GameObject("TrafficIncidentSystem");
            go.AddComponent<TrafficIncidentSystem>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void ReportCollision(Vector3 worldPosition, Lane lane, Lane otherLane, TrafficCarAI primaryVehicle, TrafficCarAI secondaryVehicle, TrafficIntersectionManager intersection = null)
        {
            Report(new TrafficIncidentData(TrafficIncidentType.Collision, worldPosition, lane, otherLane, primaryVehicle, secondaryVehicle, intersection));
        }

        public static void ReportRedLightViolation(Vector3 worldPosition, Lane lane, TrafficCarAI vehicle, TrafficIntersectionManager intersection = null)
        {
            Report(new TrafficIncidentData(TrafficIncidentType.RedLightViolation, worldPosition, lane, null, vehicle, null, intersection));
        }

        private static void Report(TrafficIncidentData incident)
        {
            GlobalIncidentCount++;
            if (incident.Type == TrafficIncidentType.Collision) GlobalCollisionCount++;
            else if (incident.Type == TrafficIncidentType.RedLightViolation) GlobalRedLightViolationCount++;
            IncidentReported?.Invoke(incident);
        }
    }
}
