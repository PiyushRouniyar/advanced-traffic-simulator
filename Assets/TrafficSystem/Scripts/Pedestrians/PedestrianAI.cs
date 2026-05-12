using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianAI : MonoBehaviour
    {
        [SerializeField] private PedestrianLane startLane;
        [SerializeField] private int startWaypointIndex;
        [SerializeField] private float minWalkSpeed = 1.0f;
        [SerializeField] private float maxWalkSpeed = 1.8f;
        [SerializeField] private float rotationLerp = 7f;
        [SerializeField] private float reachDistance = 0.4f;
        [SerializeField] private Vector2 randomPauseRange = new Vector2(0f, 1.2f);
        [SerializeField] private CitizenPathDecider pathDecider;

        private PedestrianLane currentLane;
        private PedestrianLane previousLane;
        private int currentIndex;
        private PedestrianWaypoint targetWaypoint;
        private float currentWalkSpeed;
        private float pauseTimer;
        private PedestrianCrossingZone activeCrossingZone;

        public void SetStartLane(PedestrianLane lane)
        {
            startLane = lane;
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (currentLane == null || targetWaypoint == null)
            {
                return;
            }

            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                return;
            }

            bool waitingForCrossSignal = targetWaypoint.CrossingZone != null &&
                                         activeCrossingZone == null &&
                                         !targetWaypoint.CrossingZone.CanPedestriansCross;
            if (waitingForCrossSignal)
            {
                return;
            }

            Vector3 targetPos = targetWaypoint.transform.position;
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                ReachWaypoint();
                return;
            }

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationLerp * Time.deltaTime);
            transform.position += transform.forward * currentWalkSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, targetPos) <= reachDistance)
            {
                ReachWaypoint();
            }
        }

        private void ReachWaypoint()
        {
            HandleCrossingZoneTransition(targetWaypoint);
            ApplyWaypointPause(targetWaypoint);

            if (currentLane.TryGetNextWaypoint(currentIndex, out PedestrianWaypoint next))
            {
                currentIndex = (currentIndex + 1) % Mathf.Max(1, currentLane.Waypoints.Count);
                targetWaypoint = next;
                return;
            }

            PedestrianLane nextLane = currentLane.GetRandomConnectedLane();
            if (pathDecider != null)
            {
                nextLane = pathDecider.GetNextLane(currentLane, previousLane);
            }

            if (nextLane != null)
            {
                nextLane.RefreshWaypointsFromChildren();
                if (nextLane.StartWaypoint != null)
                {
                    previousLane = currentLane;
                    currentLane = nextLane;
                    currentIndex = 0;
                    targetWaypoint = nextLane.StartWaypoint;
                    return;
                }
            }

            currentIndex = 0;
            targetWaypoint = currentLane.StartWaypoint;
        }

        private void Initialize()
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
            previousLane = null;
            currentIndex = Mathf.Clamp(startWaypointIndex, 0, currentLane.Waypoints.Count - 1);
            targetWaypoint = currentLane.Waypoints[currentIndex];
            currentWalkSpeed = Random.Range(Mathf.Max(0.2f, minWalkSpeed), Mathf.Max(minWalkSpeed, maxWalkSpeed));
            transform.position = targetWaypoint.transform.position;

            if (pathDecider == null)
            {
                pathDecider = GetComponent<CitizenPathDecider>();
            }
        }

        private void ApplyWaypointPause(PedestrianWaypoint waypoint)
        {
            if (waypoint == null)
            {
                return;
            }

            if (waypoint.WaitAtWaypoint)
            {
                pauseTimer = Random.Range(waypoint.MinWaitTime, waypoint.MaxWaitTime);
                return;
            }

            pauseTimer = Random.Range(Mathf.Max(0f, randomPauseRange.x), Mathf.Max(randomPauseRange.x, randomPauseRange.y));
        }

        private void HandleCrossingZoneTransition(PedestrianWaypoint waypoint)
        {
            if (waypoint == null)
            {
                return;
            }

            PedestrianCrossingZone targetZone = waypoint.CrossingZone;
            if (activeCrossingZone != null && activeCrossingZone != targetZone)
            {
                activeCrossingZone.ExitCrossing(this);
                activeCrossingZone = null;
            }

            if (targetZone != null)
            {
                targetZone.EnterCrossing(this);
                activeCrossingZone = targetZone;
            }
        }

        private void OnDisable()
        {
            if (activeCrossingZone != null)
            {
                activeCrossingZone.ExitCrossing(this);
                activeCrossingZone = null;
            }
        }
    }
}
