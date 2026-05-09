using MyTrafficSystem.Vehicles;
using MyTrafficSystem.Waypoints;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    /// <summary>
    /// Performs safe overtakes and returns cars to their original lane when possible.
    /// </summary>
    public class LaneChangeAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarWaypointFollower carFollower;
        [SerializeField] private LaneDetector laneDetector;
        [SerializeField] private SafeOvertake safeOvertake;

        [Header("Behavior")]
        [SerializeField] private float laneChangeSpeed = 1f;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float overtakeCooldown = 3f;

        private WaypointPath originalLane;
        private float cooldownTimer;
        private bool isOvertaking;

        private void Start()
        {
            if (carFollower == null)
            {
                carFollower = GetComponent<CarWaypointFollower>();
            }

            if (laneDetector == null)
            {
                laneDetector = GetComponent<LaneDetector>();
            }

            if (safeOvertake == null)
            {
                safeOvertake = GetComponent<SafeOvertake>();
            }

            if (carFollower != null)
            {
                originalLane = carFollower.CurrentPath;
            }

            ApplyTuning();
        }

        private void Update()
        {
            if (carFollower == null || laneDetector == null || safeOvertake == null)
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;
            ApplyTuning();

            if (!isOvertaking)
            {
                TryStartOvertake();
                return;
            }

            TryReturnToOriginalLane();
        }

        private void TryStartOvertake()
        {
            if (cooldownTimer > 0f)
            {
                return;
            }

            if (!laneDetector.HasVehicleAhead())
            {
                return;
            }

            if (!safeOvertake.TryGetBestOvertakeLane(out WaypointPath targetLane))
            {
                return;
            }

            if (targetLane == carFollower.CurrentPath)
            {
                return;
            }

            if (carFollower.TrySwitchToPath(targetLane))
            {
                isOvertaking = true;
                carFollower.SetExternalTurnMultiplier(laneChangeSpeed);
                cooldownTimer = Mathf.Max(0.1f, overtakeCooldown);
            }
        }

        private void TryReturnToOriginalLane()
        {
            if (cooldownTimer > 0f || originalLane == null)
            {
                return;
            }

            if (!safeOvertake.IsSafeToReturn(originalLane))
            {
                return;
            }

            if (carFollower.TrySwitchToPath(originalLane))
            {
                isOvertaking = false;
                carFollower.SetExternalTurnMultiplier(1f);
                cooldownTimer = Mathf.Max(0.1f, overtakeCooldown);
            }
        }

        private void ApplyTuning()
        {
            if (laneDetector != null)
            {
                laneDetector.SetDetectionRange(detectionRange);
            }

            if (!isOvertaking && carFollower != null)
            {
                carFollower.SetExternalTurnMultiplier(1f);
            }
        }

        private void OnValidate()
        {
            // Keep values positive and mirror detection range into the lane detector.
            laneChangeSpeed = Mathf.Max(0.1f, laneChangeSpeed);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            overtakeCooldown = Mathf.Max(0.1f, overtakeCooldown);

            if (laneDetector != null)
            {
                laneDetector.SetDetectionRange(detectionRange);
            }
        }
    }
}
