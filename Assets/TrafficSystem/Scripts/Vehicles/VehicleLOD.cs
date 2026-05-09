using MyTrafficSystem.AI;
using MyTrafficSystem.Managers;
using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Reduces per-vehicle simulation cost based on distance to viewer.
    /// </summary>
    public class VehicleLOD : MonoBehaviour
    {
        public enum LODLevel
        {
            Full = 0,
            Reduced = 1,
            Culled = 2
        }

        [Header("Distance Thresholds")]
        [SerializeField] private float fullSimulationDistance = 55f;
        [SerializeField] private float reducedSimulationDistance = 120f;

        [Header("Core References")]
        [SerializeField] private CarWaypointFollower carFollower;
        [SerializeField] private Rigidbody carRigidbody;
        [SerializeField] private DistanceCullingSystem distanceCullingSystem;

        [Header("Optional High Cost Components")]
        [SerializeField] private ObstacleDetection obstacleDetection;
        [SerializeField] private LaneChangeAI laneChangeAI;
        [SerializeField] private IndicatorController indicatorController;
        [SerializeField] private BrakeLightController brakeLightController;

        public LODLevel CurrentLevel { get; private set; } = LODLevel.Full;

        private void Awake()
        {
            if (carFollower == null)
            {
                carFollower = GetComponent<CarWaypointFollower>();
            }

            if (carRigidbody == null)
            {
                carRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void OnEnable()
        {
            TrafficOptimizationManager.Instance?.RegisterVehicle(this);
        }

        private void OnDisable()
        {
            TrafficOptimizationManager.Instance?.UnregisterVehicle(this);
        }

        public void ApplyDistanceLOD(float distanceToViewer)
        {
            LODLevel targetLevel = ResolveTargetLevel(distanceToViewer);
            if (targetLevel == CurrentLevel)
            {
                distanceCullingSystem?.ApplyCulling(distanceToViewer);
                return;
            }

            CurrentLevel = targetLevel;
            ApplyLevelState(distanceToViewer);
        }

        private LODLevel ResolveTargetLevel(float distanceToViewer)
        {
            if (distanceCullingSystem != null && distanceToViewer > distanceCullingSystem.CullDistance)
            {
                return LODLevel.Culled;
            }

            if (distanceToViewer > Mathf.Max(fullSimulationDistance + 1f, reducedSimulationDistance))
            {
                return LODLevel.Reduced;
            }

            return LODLevel.Full;
        }

        private void ApplyLevelState(float distanceToViewer)
        {
            bool full = CurrentLevel == LODLevel.Full;
            bool reduced = CurrentLevel == LODLevel.Reduced;
            bool culled = CurrentLevel == LODLevel.Culled;

            if (obstacleDetection != null)
            {
                obstacleDetection.enabled = full;
            }

            if (laneChangeAI != null)
            {
                laneChangeAI.enabled = full;
            }

            if (indicatorController != null)
            {
                indicatorController.enabled = full || reduced;
            }

            if (brakeLightController != null)
            {
                brakeLightController.enabled = full || reduced;
            }

            if (carFollower != null)
            {
                if (culled)
                {
                    carFollower.SetSimulationPaused(true);
                }
                else
                {
                    carFollower.SetSimulationPaused(false);
                    carFollower.SetExternalSpeedMultiplier(reduced ? 0.75f : 1f);
                }
            }

            if (carRigidbody != null)
            {
                carRigidbody.isKinematic = culled;
                carRigidbody.detectCollisions = !culled;
                carRigidbody.interpolation = full ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }

            distanceCullingSystem?.ApplyCulling(distanceToViewer);
        }
    }
}
