using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    [DisallowMultipleComponent]
    public class TrafficLaneFollower : MonoBehaviour
    {
        [SerializeField] private Lane startLane;
        [SerializeField] private int startWaypointIndex;
        [SerializeField] private float speedOverride = -1f;
        [SerializeField] private float steeringLerp = 6f;
        [SerializeField] private float reachDistance = 1.2f;

        private Lane currentLane;
        private int currentIndex;
        private Waypoint currentTarget;

        public Lane CurrentLane => currentLane;

        private void Start()
        {
            if (startLane == null)
            {
                enabled = false;
                return;
            }

            startLane.RefreshWaypointsFromChildren();
            if (startLane.Waypoints.Count == 0)
            {
                enabled = false;
                return;
            }

            currentLane = startLane;
            currentIndex = Mathf.Clamp(startWaypointIndex, 0, currentLane.Waypoints.Count - 1);
            currentTarget = currentLane.Waypoints[currentIndex];
        }

        private void Update()
        {
            if (currentLane == null)
            {
                return;
            }

            if (currentTarget == null)
            {
                currentLane.RefreshWaypointsFromChildren();
                if (currentLane.Waypoints.Count == 0)
                {
                    enabled = false;
                    return;
                }

                currentIndex = Mathf.Clamp(currentIndex, 0, currentLane.Waypoints.Count - 1);
                currentTarget = currentLane.Waypoints[currentIndex];
                if (currentTarget == null)
                {
                    return;
                }
            }

            Vector3 toTarget = currentTarget.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                Advance();
                return;
            }

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, steeringLerp * Time.deltaTime);

            float speed = speedOverride > 0f ? speedOverride : currentLane.SpeedLimit;
            transform.position += transform.forward * speed * Time.deltaTime;

            if (Vector3.Distance(transform.position, currentTarget.transform.position) <= reachDistance)
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (currentLane == null)
            {
                enabled = false;
                return;
            }

            if (currentLane.TryGetNextWaypoint(currentIndex, out Waypoint next))
            {
                currentIndex = (currentIndex + 1) % Mathf.Max(1, currentLane.Waypoints.Count);
                currentTarget = next;
                return;
            }

            Lane branch = currentLane.GetRandomConnectedLane();
            if (branch != null)
            {
                branch.RefreshWaypointsFromChildren();
                if (branch.StartWaypoint != null)
                {
                    currentLane = branch;
                    currentIndex = 0;
                    currentTarget = branch.StartWaypoint;
                    return;
                }
            }

            enabled = false;
        }
    }
}
