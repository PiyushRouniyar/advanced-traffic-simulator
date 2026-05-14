using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenAI : MonoBehaviour
    {
        public enum CitizenState { Waiting, Checking, Crossing, Blocked, Walking, Idle }

        [Header("Lane")]
        [SerializeField] private CitizenLane startLane;
        [SerializeField] private int startWaypointIndex;
        [SerializeField] private bool useConnectedLanes = true;

        [Header("Movement")]
        [SerializeField] private float minWalkSpeed = 1f;
        [SerializeField] private float maxWalkSpeed = 1.8f;
        [SerializeField] private float acceleration = 3.5f;
        [SerializeField] private float rotationLerp = 7f;
        [SerializeField] private float waypointReachDistance = 0.35f;

        [Header("Behavior")]
        [SerializeField] private Vector2 randomPauseRange = new Vector2(0f, 1.1f);
        [SerializeField] private float trafficCheckInterval = 0.1f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string waitingParam = "IsWaiting";

        [Header("Debug")]
        [SerializeField] private bool showStateLabel = true;

        private CitizenLane currentLane;
        private int currentIndex;
        private CitizenWaypoint targetWaypoint;
        private float targetWalkSpeed;
        private float currentWalkSpeed;
        private float pauseTimer;
        private float trafficCheckTimer;
        private bool cachedCanCross = true;
        private CitizenCrossingNode activeCrossingNode;
        private bool isRegisteredInCrossingZone;
        private CitizenState state;

        public CitizenLane CurrentLane => currentLane;
        public CitizenState State => state;

        public void SetStartLane(CitizenLane lane)
        {
            startLane = lane;
            if (Application.isPlaying) Initialize();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Start() => Initialize();

        private void Update()
        {
            if (currentLane == null || targetWaypoint == null) return;

            if (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                SetState(CitizenState.Idle);
                UpdateAnimator();
                return;
            }

            if (ShouldWaitForCrossing())
            {
                currentWalkSpeed = Mathf.MoveTowards(currentWalkSpeed, 0f, acceleration * Time.deltaTime);
                UpdateAnimator();
                return;
            }

            MoveToWaypoint();
        }

        private void Initialize()
        {
            if (startLane == null)
            {
                startLane = FindFirstObjectByType<CitizenLane>(FindObjectsInactive.Exclude);
            }
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
            targetWaypoint = currentLane.Waypoints[currentIndex];
            transform.position = targetWaypoint.transform.position;

            targetWalkSpeed = Random.Range(Mathf.Max(0.2f, minWalkSpeed), Mathf.Max(minWalkSpeed, maxWalkSpeed));
            currentWalkSpeed = 0f;
            pauseTimer = 0f;
            SetState(CitizenState.Idle);
            UpdateAnimator();
            enabled = true;
        }

        private bool ShouldWaitForCrossing()
        {
            if (currentLane != null && currentLane.ShouldStopAtAssignedLight(currentIndex))
            {
                UnregisterFromCrossingIfNeeded();
                SetState(CitizenState.Waiting);
                return true;
            }

            if (!targetWaypoint.RequiresCrossingCheck)
            {
                UnregisterFromCrossingIfNeeded();
                return false;
            }

            SetState(CitizenState.Checking);
            trafficCheckTimer -= Time.deltaTime;
            if (trafficCheckTimer <= 0f)
            {
                trafficCheckTimer = Mathf.Max(0.02f, trafficCheckInterval);
                cachedCanCross = targetWaypoint.CrossingNode == null || targetWaypoint.CrossingNode.IsCrossingSafe(transform.position);
            }

            if (!cachedCanCross)
            {
                SetState(CitizenState.Blocked);
                UnregisterFromCrossingIfNeeded();
                return true;
            }

            if (targetWaypoint.CrossingNode != null)
            {
                EnsureCrossingRegistration(targetWaypoint.CrossingNode);
                SetState(CitizenState.Crossing);
            }

            return false;
        }

        private void MoveToWaypoint()
        {
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

            currentWalkSpeed = Mathf.MoveTowards(currentWalkSpeed, targetWalkSpeed, acceleration * Time.deltaTime);
            transform.position += transform.forward * currentWalkSpeed * Time.deltaTime;
            if (state != CitizenState.Crossing)
            {
                SetState(CitizenState.Walking);
            }
            UpdateAnimator();

            if (Vector3.Distance(transform.position, targetPos) <= waypointReachDistance)
            {
                ReachWaypoint();
            }
        }

        private void ReachWaypoint()
        {
            pauseTimer = targetWaypoint.WaitAtWaypoint
                ? Random.Range(targetWaypoint.MinWaitTime, targetWaypoint.MaxWaitTime)
                : Random.Range(Mathf.Max(0f, randomPauseRange.x), Mathf.Max(randomPauseRange.x, randomPauseRange.y));

            if (currentLane.TryGetNextWaypoint(currentIndex, out CitizenWaypoint next))
            {
                currentIndex = (currentIndex + 1) % Mathf.Max(1, currentLane.Waypoints.Count);
                targetWaypoint = next;
                if (!targetWaypoint.RequiresCrossingCheck)
                {
                    UnregisterFromCrossingIfNeeded();
                }
                return;
            }

            if (useConnectedLanes)
            {
                CitizenLane nextLane = currentLane.GetRandomConnectedLane();
                if (nextLane != null)
                {
                    nextLane.RefreshWaypointsFromChildren();
                    if (nextLane.StartWaypoint != null)
                    {
                        currentLane = nextLane;
                        currentIndex = 0;
                        targetWaypoint = nextLane.StartWaypoint;
                        if (targetWaypoint == null || !targetWaypoint.RequiresCrossingCheck)
                        {
                            UnregisterFromCrossingIfNeeded();
                        }
                        return;
                    }
                }
            }

            currentIndex = 0;
            targetWaypoint = currentLane.StartWaypoint;
            if (targetWaypoint == null || !targetWaypoint.RequiresCrossingCheck)
            {
                UnregisterFromCrossingIfNeeded();
            }
        }

        private void EnsureCrossingRegistration(CitizenCrossingNode node)
        {
            if (node == null)
            {
                UnregisterFromCrossingIfNeeded();
                return;
            }

            int citizenId = GetInstanceID();
            if (activeCrossingNode != node)
            {
                UnregisterFromCrossingIfNeeded();
                activeCrossingNode = node;
                PedestrianCrossingZone.ReportCitizenCrossing(activeCrossingNode, citizenId, true);
                isRegisteredInCrossingZone = true;
                return;
            }

            if (!isRegisteredInCrossingZone)
            {
                PedestrianCrossingZone.ReportCitizenCrossing(activeCrossingNode, citizenId, true);
                isRegisteredInCrossingZone = true;
            }
        }

        private void UnregisterFromCrossingIfNeeded()
        {
            if (activeCrossingNode == null || !isRegisteredInCrossingZone)
            {
                activeCrossingNode = null;
                isRegisteredInCrossingZone = false;
                return;
            }

            PedestrianCrossingZone.ReportCitizenCrossing(activeCrossingNode, GetInstanceID(), false);
            activeCrossingNode = null;
            isRegisteredInCrossingZone = false;
        }

        private void SetState(CitizenState newState) => state = newState;

        private void UpdateAnimator()
        {
            if (animator == null) return;
            float normalizedSpeed = Mathf.InverseLerp(0f, Mathf.Max(0.01f, maxWalkSpeed), currentWalkSpeed);
            animator.SetFloat(speedParam, normalizedSpeed);
            animator.SetBool(waitingParam, state == CitizenState.Waiting || state == CitizenState.Blocked || state == CitizenState.Checking);
        }

        private void OnDisable()
        {
            UnregisterFromCrossingIfNeeded();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showStateLabel || !CitizenDebugSettings.ShowDebug) return;
            if ((state == CitizenState.Waiting || state == CitizenState.Blocked) && !CitizenDebugSettings.ShowWaitingCitizens) return;

            UnityEditor.Handles.color =
                state == CitizenState.Blocked ? new Color(1f, 0.4f, 0.25f, 1f) :
                state == CitizenState.Checking ? Color.cyan :
                state == CitizenState.Waiting ? Color.yellow :
                state == CitizenState.Crossing ? new Color(0.35f, 1f, 0.55f, 1f) :
                state == CitizenState.Walking ? Color.green :
                Color.white;
            string laneName = currentLane != null ? currentLane.LaneName : "None";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"{state}\nLane: {laneName}");
        }
#endif
    }
}
