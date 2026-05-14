using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianAI : MonoBehaviour
    {
        public enum PedestrianState { Idle, Walking, Waiting }

        [Header("Path")]
        [SerializeField] private PedestrianLane startLane;
        [SerializeField] private int startWaypointIndex;
        [SerializeField] private bool useConnectedLanes = true;

        [Header("Movement")]
        [SerializeField] private float minWalkSpeed = 1.0f;
        [SerializeField] private float maxWalkSpeed = 1.8f;
        [SerializeField] private float acceleration = 3.6f;
        [SerializeField] private float rotationLerp = 7f;
        [SerializeField] private float reachDistance = 0.4f;

        [Header("Idle")]
        [SerializeField] private Vector2 randomPauseRange = new Vector2(0f, 1.2f);

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string waitingParam = "IsWaiting";

        [Header("Debug")]
        [SerializeField] private bool showStateLabel = true;

        private PedestrianLane currentLane;
        private int currentIndex;
        private PedestrianWaypoint targetWaypoint;
        private float targetWalkSpeed;
        private float currentWalkSpeed;
        private float pauseTimer;
        private PedestrianState state;

        public PedestrianLane CurrentLane => currentLane;
        public PedestrianState State => state;
        public bool IsWaiting => state == PedestrianState.Waiting;

        public void SetStartLane(PedestrianLane lane)
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
                SetState(PedestrianState.Idle);
                UpdateAnimator();
                return;
            }

            if (targetWaypoint.RequiresCrosswalkCheck && !targetWaypoint.CrosswalkNode.CanPedestriansCross)
            {
                currentWalkSpeed = Mathf.MoveTowards(currentWalkSpeed, 0f, acceleration * Time.deltaTime);
                SetState(PedestrianState.Waiting);
                UpdateAnimator();
                return;
            }

            MoveToWaypoint();
        }

        private void Initialize()
        {
            if (startLane == null)
            {
                startLane = FindFirstObjectByType<PedestrianLane>(FindObjectsInactive.Exclude);
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
            SetState(PedestrianState.Idle);
            UpdateAnimator();
            enabled = true;
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
            SetState(PedestrianState.Walking);
            UpdateAnimator();

            if (Vector3.Distance(transform.position, targetPos) <= reachDistance)
            {
                ReachWaypoint();
            }
        }

        private void ReachWaypoint()
        {
            pauseTimer = targetWaypoint.WaitAtWaypoint
                ? Random.Range(targetWaypoint.MinWaitTime, targetWaypoint.MaxWaitTime)
                : Random.Range(Mathf.Max(0f, randomPauseRange.x), Mathf.Max(randomPauseRange.x, randomPauseRange.y));

            if (currentLane.TryGetNextWaypoint(currentIndex, out PedestrianWaypoint next))
            {
                currentIndex = (currentIndex + 1) % Mathf.Max(1, currentLane.Waypoints.Count);
                targetWaypoint = next;
                return;
            }

            if (useConnectedLanes)
            {
                PedestrianLane nextLane = currentLane.GetRandomConnectedLane();
                if (nextLane != null)
                {
                    nextLane.RefreshWaypointsFromChildren();
                    if (nextLane.StartWaypoint != null)
                    {
                        currentLane = nextLane;
                        currentIndex = 0;
                        targetWaypoint = nextLane.StartWaypoint;
                        return;
                    }
                }
            }

            currentIndex = 0;
            targetWaypoint = currentLane.StartWaypoint;
        }

        private void SetState(PedestrianState newState) => state = newState;

        private void UpdateAnimator()
        {
            if (animator == null) return;
            float normalizedSpeed = Mathf.InverseLerp(0f, Mathf.Max(0.01f, maxWalkSpeed), currentWalkSpeed);
            animator.SetFloat(speedParam, normalizedSpeed);
            animator.SetBool(waitingParam, state == PedestrianState.Waiting);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showStateLabel || !PedestrianDebugSettings.ShowDebug) return;
            UnityEditor.Handles.color = state == PedestrianState.Waiting ? Color.yellow : state == PedestrianState.Walking ? Color.green : Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, state.ToString());
        }
#endif
    }
}
