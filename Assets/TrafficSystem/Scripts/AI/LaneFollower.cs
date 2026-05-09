using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    [DisallowMultipleComponent]
    public class LaneFollower : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float maxSpeed = 14f;
        [SerializeField] private float acceleration = 6f;
        [SerializeField] private float braking = 10f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float waypointReachDistance = 1.25f;

        private Lane currentLane;
        private int currentWaypointIndex;
        private float currentSpeed;

        public Lane CurrentLane => currentLane;
        public int CurrentWaypointIndex => currentWaypointIndex;
        public Waypoint CurrentTarget =>
            currentLane != null && currentLane.Waypoints.Count > 0 && currentWaypointIndex >= 0 && currentWaypointIndex < currentLane.Waypoints.Count
                ? currentLane.Waypoints[currentWaypointIndex]
                : null;

        private void Awake()
        {
            if (rb == null) { rb = GetComponent<Rigidbody>(); }
        }

        public void Initialize(Lane lane, int startIndex)
        {
            currentLane = lane;
            if (currentLane != null)
            {
                currentLane.RefreshWaypointsFromChildren();
            }

            currentWaypointIndex = Mathf.Clamp(startIndex, 0, currentLane != null ? Mathf.Max(0, currentLane.Waypoints.Count - 1) : 0);
            currentSpeed = 0f;
        }

        public void Configure(float maxSpd, float accel, float brake, float turn)
        {
            maxSpeed = Mathf.Max(1f, maxSpd);
            acceleration = Mathf.Max(0.1f, accel);
            braking = Mathf.Max(0.1f, brake);
            turnSpeed = Mathf.Max(0.1f, turn);
        }

        public bool Step(float desiredSpeed, bool brakeHard)
        {
            if (currentLane == null || rb == null)
            {
                return false;
            }

            currentLane.RefreshWaypointsFromChildren();
            if (currentLane.Waypoints.Count == 0)
            {
                return false;
            }

            currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, currentLane.Waypoints.Count - 1);
            Waypoint target = currentLane.Waypoints[currentWaypointIndex];
            if (target == null)
            {
                return false;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));
            }

            float laneSpeed = currentLane.SpeedLimit;
            float targetSpeed = Mathf.Min(maxSpeed, desiredSpeed, laneSpeed);
            float change = brakeHard ? braking : (targetSpeed < currentSpeed ? braking : acceleration);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, change * Time.fixedDeltaTime);
            Vector3 velocity = transform.forward * currentSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            return Vector3.Distance(transform.position, target.transform.position) <= waypointReachDistance;
        }

        public bool AdvanceWithinLane()
        {
            if (currentLane == null)
            {
                return false;
            }

            if (currentLane.TryGetNextWaypoint(currentWaypointIndex, out _))
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= currentLane.Waypoints.Count) { currentWaypointIndex = 0; }
                return true;
            }
            return false;
        }

        public void SetLane(Lane lane, int startIndex)
        {
            currentLane = lane;
            if (currentLane != null)
            {
                currentLane.RefreshWaypointsFromChildren();
            }
            currentWaypointIndex = Mathf.Clamp(startIndex, 0, currentLane != null ? Mathf.Max(0, currentLane.Waypoints.Count - 1) : 0);
        }
    }
}
