using MyTrafficSystem.Lanes;
using MyTrafficSystem.Pedestrians;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class TrafficCarAI : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private Lane startLane;

        [Header("Movement")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float turnSpeed = 6f;
        [SerializeField] private float detectionDistance = 8f;
        [SerializeField] private float crosswalkStopDistance = 7f;
        [SerializeField] private float waypointReachDistance = 1.2f;
        [SerializeField] private float minTurnSpeedFactor = 0.35f;

        [Header("Obstacle Detection")]
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private TrafficRouteDecider routeDecider;

        private Rigidbody rb;
        private Lane currentLane;
        private int waypointIndex;
        private float currentSpeed;
        private bool lastStopForLight;

        public Lane CurrentLane => currentLane;
        public int CurrentWaypointIndex => waypointIndex;
        public bool IsStoppedByAssignedLight => lastStopForLight;

        public void SetStartLane(Lane lane)
        {
            startLane = lane;
            if (Application.isPlaying)
            {
                InitializeLane();
            }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (routeDecider == null)
            {
                routeDecider = GetComponent<TrafficRouteDecider>();
            }
        }

        private void Start()
        {
            InitializeLane();
        }

        private void InitializeLane()
        {
            if (startLane == null || startLane.Waypoints.Count == 0)
            {
                enabled = false;
                return;
            }

            currentLane = startLane;
            waypointIndex = 0;
            transform.position = currentLane.Waypoints[0].transform.position;
            enabled = true;
        }

        private void FixedUpdate()
        {
            if (currentLane == null || currentLane.Waypoints.Count == 0)
            {
                return;
            }

            currentLane.RefreshWaypointsFromChildren();
            if (currentLane.Waypoints.Count == 0)
            {
                currentSpeed = 0f;
                rb.linearVelocity = Vector3.zero;
                return;
            }

            Waypoint target = currentLane.Waypoints[Mathf.Clamp(waypointIndex, 0, currentLane.Waypoints.Count - 1)];
            if (target == null)
            {
                currentSpeed = 0f;
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            Vector3 forwardFlat = transform.forward;
            forwardFlat.y = 0f;
            float turnAngle = toTarget.sqrMagnitude > 0.001f ? Vector3.Angle(forwardFlat.normalized, toTarget.normalized) : 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
            }

            bool stopForLight = currentLane.ShouldStopAtLight(waypointIndex);
            lastStopForLight = stopForLight;
            bool stopForCar = DetectCarAhead(out float hitDistance);
            bool stopForCrosswalk = PedestrianCrossingZone.IsCrosswalkBlockingCars(transform.position, transform.forward, crosswalkStopDistance);

            float turnSlowFactor = Mathf.Lerp(1f, minTurnSpeedFactor, Mathf.InverseLerp(20f, 90f, turnAngle));
            float desiredSpeed = Mathf.Min(speed, currentLane.Speed) * turnSlowFactor;
            if (stopForCar)
            {
                desiredSpeed *= Mathf.Clamp01(hitDistance / Mathf.Max(0.1f, detectionDistance));
            }

            bool shouldStop = stopForLight || stopForCar || stopForCrosswalk;
            float accel = shouldStop ? 10f : 6f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, shouldStop ? 0f : desiredSpeed, accel * Time.fixedDeltaTime);

            Vector3 velocity = transform.forward * currentSpeed;
            velocity.y = rb.linearVelocity.y;
            rb.linearVelocity = velocity;

            if (Vector3.Distance(transform.position, target.transform.position) <= waypointReachDistance)
            {
                AdvanceLaneProgress();
            }
        }

        private bool DetectCarAhead(out float distance)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, detectionDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance;
                if (hit.rigidbody == null || hit.rigidbody == rb)
                {
                    return false;
                }

                TrafficCarAI otherCar = hit.rigidbody.GetComponent<TrafficCarAI>();
                if (otherCar == null)
                {
                    otherCar = hit.rigidbody.GetComponentInParent<TrafficCarAI>();
                }

                return otherCar != null;
            }

            distance = detectionDistance;
            return false;
        }

        private void AdvanceLaneProgress()
        {
            int next = waypointIndex + 1;
            if (next < currentLane.Waypoints.Count)
            {
                waypointIndex = next;
                return;
            }

            if (currentLane.Loop && currentLane.Waypoints.Count > 0)
            {
                waypointIndex = 0;
                return;
            }

            Lane nextLane = routeDecider != null ? routeDecider.DecideNextLane(currentLane) : currentLane.GetRandomConnectedLane();
            if (nextLane != null && nextLane.Waypoints.Count > 0)
            {
                nextLane.RefreshWaypointsFromChildren();
                currentLane = nextLane;
                waypointIndex = 0;
                return;
            }

            currentSpeed = 0f;
            rb.linearVelocity = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * detectionDistance);

#if UNITY_EDITOR
            string laneName = currentLane != null ? currentLane.LaneName : "None";
            string groupName = "N/A";
            string obey = lastStopForLight ? "STOP (Assigned Red)" : "GO";
            UnityEditor.Handles.color = lastStopForLight ? Color.red : Color.green;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.2f, $"Lane: {laneName}\nLight Group: {groupName}\nObeying: {obey}");
#endif
        }
    }
}
