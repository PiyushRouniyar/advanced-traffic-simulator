using MyTrafficSystem.Waypoints;
using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Moves a car along a waypoint path with smooth turning and forward movement.
    /// </summary>
    public class CarWaypointFollower : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private WaypointPath waypointPath;
        [SerializeField] private int startingWaypointIndex = 0;

        [Header("Vehicle Setup")]
        [SerializeField] private VehicleConfiguration vehicleConfiguration;
        [SerializeField] private Rigidbody carRigidbody;
        [SerializeField] private ObstacleDetection obstacleDetection;

        [Header("Wheel Transforms (Optional Visuals)")]
        [SerializeField] private Transform frontLeftWheel;
        [SerializeField] private Transform frontRightWheel;
        [SerializeField] private Transform rearLeftWheel;
        [SerializeField] private Transform rearRightWheel;

        [Header("Override Movement (Used if no Vehicle Configuration)")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float braking = 12f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float stoppingDistance = 1.5f;

        private int currentWaypointIndex;
        private Transform currentTarget;
        private bool reachedPathEnd;
        private float currentSpeed;
        private int trafficLightStopRequests;
        private Waypoint currentWaypoint;
        private float externalTurnMultiplier = 1f;
        private float externalSpeedMultiplier = 1f;
        private float combinedSpeedMultiplier = 1f;
        private bool isBraking;

        private float driverMaxSpeedMultiplier = 1f;
        private float driverAccelerationMultiplier = 1f;
        private float driverBrakingMultiplier = 1f;
        private float driverReactionTime = 0.25f;
        private bool simulationPaused;

        public WaypointPath CurrentPath => waypointPath;
        public float CurrentSpeed => currentSpeed;
        public bool IsBraking => isBraking;

        public void ConfigurePath(WaypointPath path, int startIndex)
        {
            waypointPath = path;
            startingWaypointIndex = Mathf.Max(0, startIndex);

            reachedPathEnd = false;
            currentSpeed = 0f;
            trafficLightStopRequests = 0;
            combinedSpeedMultiplier = 1f;
            isBraking = false;

            if (waypointPath == null || waypointPath.WaypointCount == 0)
            {
                currentTarget = null;
                currentWaypoint = null;
                return;
            }

            currentWaypointIndex = Mathf.Clamp(startingWaypointIndex, 0, waypointPath.WaypointCount - 1);
            SetCurrentTarget();
        }

        public bool TrySwitchToPath(WaypointPath newPath)
        {
            if (newPath == null || newPath.WaypointCount == 0)
            {
                return false;
            }

            int closestIndex = newPath.GetClosestWaypointIndex(transform.position);
            if (closestIndex < 0)
            {
                return false;
            }

            waypointPath = newPath;
            reachedPathEnd = false;
            currentWaypointIndex = closestIndex;
            SetCurrentTarget();
            return true;
        }

        public void SetExternalTurnMultiplier(float multiplier)
        {
            externalTurnMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void SetExternalSpeedMultiplier(float multiplier)
        {
            externalSpeedMultiplier = Mathf.Clamp01(multiplier);
        }

        public void ApplyDriverBehavior(float maxSpeedMultiplier, float accelerationMultiplier, float brakingMultiplier, float reactionTime)
        {
            driverMaxSpeedMultiplier = Mathf.Max(0.2f, maxSpeedMultiplier);
            driverAccelerationMultiplier = Mathf.Max(0.2f, accelerationMultiplier);
            driverBrakingMultiplier = Mathf.Max(0.2f, brakingMultiplier);
            driverReactionTime = Mathf.Max(0.01f, reactionTime);
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;

            if (paused && carRigidbody != null)
            {
                carRigidbody.linearVelocity = Vector3.zero;
                carRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void Start()
        {
            if (!TrySetupPath())
            {
                enabled = false;
                return;
            }

            if (carRigidbody == null)
            {
                carRigidbody = GetComponent<Rigidbody>();
            }

            if (carRigidbody == null)
            {
                enabled = false;
                return;
            }

            if (obstacleDetection == null)
            {
                obstacleDetection = GetComponent<ObstacleDetection>();
            }
            ConfigurePath(waypointPath, startingWaypointIndex);
        }

        private void FixedUpdate()
        {
            if (simulationPaused)
            {
                isBraking = false;
                return;
            }

            if (reachedPathEnd || currentTarget == null)
            {
                ApplyBrakingToStop();
                return;
            }

            MoveTowardCurrentWaypoint();
            CheckIfWaypointReached();
        }

        private bool TrySetupPath()
        {
            return waypointPath != null && waypointPath.WaypointCount > 0;
        }

        private void SetCurrentTarget()
        {
            currentWaypoint = waypointPath.GetWaypoint(currentWaypointIndex);
            currentTarget = currentWaypoint != null ? currentWaypoint.transform : null;
        }

        private void MoveTowardCurrentWaypoint()
        {
            Vector3 toTarget = currentTarget.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            float effectiveTurnSpeed = ActiveTurnSpeed * externalTurnMultiplier;
            Quaternion nextRotation = Quaternion.Slerp(transform.rotation, targetRotation, effectiveTurnSpeed * Time.fixedDeltaTime);
            carRigidbody.MoveRotation(nextRotation);

            float dotToTarget = Vector3.Dot(transform.forward, toTarget.normalized);
            float forwardAlignment = Mathf.Clamp01((dotToTarget + 1f) * 0.5f);
            UpdateCombinedSpeedMultiplier();
            float targetSpeed = ActiveMaxSpeed * driverMaxSpeedMultiplier * forwardAlignment * combinedSpeedMultiplier;

            float brakingRate = GetActiveBrakingRate() * driverBrakingMultiplier;
            float accelerationRate = ActiveAcceleration * driverAccelerationMultiplier;
            float speedChange = targetSpeed < currentSpeed ? brakingRate : accelerationRate;
            isBraking = targetSpeed < currentSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange * Time.fixedDeltaTime);

            Vector3 velocity = transform.forward * currentSpeed;
            velocity.y = carRigidbody.linearVelocity.y;
            carRigidbody.linearVelocity = velocity;

            RotateWheelVisuals();
        }

        private void CheckIfWaypointReached()
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            if (distanceToTarget > ActiveStoppingDistance)
            {
                return;
            }

            if (!TryResolveNextWaypoint(out WaypointPath nextPath, out int nextIndex))
            {
                reachedPathEnd = true;
                return;
            }

            waypointPath = nextPath;
            currentWaypointIndex = nextIndex;
            SetCurrentTarget();
        }

        private bool TryResolveNextWaypoint(out WaypointPath nextPath, out int nextIndex)
        {
            nextPath = waypointPath;
            nextIndex = -1;

            if (currentWaypoint != null)
            {
                IntersectionNode intersectionNode = currentWaypoint.GetComponent<IntersectionNode>();
                if (intersectionNode != null && intersectionNode.TryGetRandomBranch(out LaneBranch branch))
                {
                    int branchIndex = branch.TargetPath.IndexOfWaypoint(branch.TargetWaypoint);
                    if (branchIndex >= 0)
                    {
                        nextPath = branch.TargetPath;
                        nextIndex = branchIndex;
                        return true;
                    }
                }
            }

            int defaultNext = waypointPath.GetNextIndex(currentWaypointIndex);
            if (defaultNext < 0)
            {
                return false;
            }

            nextIndex = defaultNext;
            return true;
        }

        private void ApplyBrakingToStop()
        {
            float brakingRate = GetActiveBrakingRate() * driverBrakingMultiplier;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakingRate * Time.fixedDeltaTime);
            isBraking = true;
            Vector3 velocity = transform.forward * currentSpeed;
            velocity.y = carRigidbody.linearVelocity.y;
            carRigidbody.linearVelocity = velocity;

            RotateWheelVisuals();
        }

        private void RotateWheelVisuals()
        {
            float wheelSpin = currentSpeed * 35f * Time.fixedDeltaTime;
            RotateWheel(frontLeftWheel, wheelSpin);
            RotateWheel(frontRightWheel, wheelSpin);
            RotateWheel(rearLeftWheel, wheelSpin);
            RotateWheel(rearRightWheel, wheelSpin);
        }

        private static void RotateWheel(Transform wheel, float spinAmount)
        {
            if (wheel == null)
            {
                return;
            }

            wheel.Rotate(Vector3.right, spinAmount, Space.Self);
        }

        private float GetObstacleSpeedMultiplier()
        {
            return obstacleDetection != null ? obstacleDetection.SpeedMultiplier : 1f;
        }

        public void SetTrafficLightStop(bool shouldStop)
        {
            if (shouldStop)
            {
                trafficLightStopRequests++;
                return;
            }

            trafficLightStopRequests = Mathf.Max(0, trafficLightStopRequests - 1);
        }

        private float GetCombinedSpeedMultiplier()
        {
            float trafficLightMultiplier = trafficLightStopRequests > 0 ? 0f : 1f;
            return trafficLightMultiplier * GetObstacleSpeedMultiplier() * externalSpeedMultiplier;
        }

        private void UpdateCombinedSpeedMultiplier()
        {
            float desiredMultiplier = GetCombinedSpeedMultiplier();
            float reactionRate = Time.fixedDeltaTime / driverReactionTime;
            combinedSpeedMultiplier = Mathf.MoveTowards(combinedSpeedMultiplier, desiredMultiplier, reactionRate);
        }

        private float GetActiveBrakingRate()
        {
            float followerBraking = ActiveBraking;
            if (obstacleDetection == null)
            {
                return followerBraking;
            }

            return Mathf.Max(followerBraking, obstacleDetection.BrakeForce);
        }

        private float ActiveMaxSpeed => vehicleConfiguration != null ? vehicleConfiguration.MaxSpeed : Mathf.Max(0f, maxSpeed);
        private float ActiveAcceleration => vehicleConfiguration != null ? vehicleConfiguration.Acceleration : Mathf.Max(0f, acceleration);
        private float ActiveBraking => vehicleConfiguration != null ? vehicleConfiguration.Braking : Mathf.Max(0f, braking);
        private float ActiveTurnSpeed => vehicleConfiguration != null ? vehicleConfiguration.TurnSpeed : Mathf.Max(0f, turnSpeed);
        private float ActiveStoppingDistance => vehicleConfiguration != null ? vehicleConfiguration.StoppingDistance : Mathf.Max(0.1f, stoppingDistance);
    }
}
