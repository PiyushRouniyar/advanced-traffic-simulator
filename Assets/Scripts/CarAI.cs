// ============================================================
//  CarAI.cs  –  Unity 6  |  Traffic Simulator
//  Attach this script to any car GameObject.
//  Does NOT use NavMesh, WheelColliders, or advanced physics.
// ============================================================

using UnityEngine;

public class CarAI : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  INSPECTOR VARIABLES
    //  Tweak these in the Unity Inspector without touching code.
    // ──────────────────────────────────────────────────────────

    [Header("Movement Settings")]
    [Tooltip("How fast the car moves along the road (units per second).")]
    public float moveSpeed = 5f;

    [Tooltip("How quickly the car rotates to face the next waypoint. Higher = snappier turn.")]
    public float rotationSpeed = 5f;

    [Tooltip("How close the car must get to a waypoint before moving on to the next one.")]
    public float waypointReachDistance = 1.5f;

    [Header("Waypoints")]
    [Tooltip("Assign Transform objects here in order. The car will follow them in sequence and loop.")]
    public Transform[] waypoints;

    [Header("Debug")]
    [Tooltip("Draw the waypoint path in the Scene View (editor only).")]
    public bool showDebugPath = true;

    // ──────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ──────────────────────────────────────────────────────────

    private int   _currentWaypointIndex = 0;   // Which waypoint we are heading toward
    private bool  _isReady              = false; // Safety flag – only move when setup is valid

    // ──────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ──────────────────────────────────────────────────────────

    private void Start()
    {
        _isReady = ValidateWaypoints();

        if (_isReady)
        {
            // Snap the car to face the first waypoint on spawn so
            // it does not spin wildly at the very start of the game.
            FaceWaypointInstant(waypoints[_currentWaypointIndex]);
        }
    }

    private void Update()
    {
        if (!_isReady) return;

        MoveTowardsCurrentWaypoint();
        CheckWaypointReached();
    }

    // ──────────────────────────────────────────────────────────
    //  MOVEMENT
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the car toward the current target waypoint each frame.
    /// Translation and rotation are handled separately for clean control.
    /// </summary>
    private void MoveTowardsCurrentWaypoint()
    {
        Transform target = waypoints[_currentWaypointIndex];

        // ── Direction & Distance ─────────────────────────────
        Vector3 targetPosition = GetFlatPosition(target.position);
        Vector3 myPosition     = GetFlatPosition(transform.position);

        Vector3 direction = (targetPosition - myPosition).normalized;

        // ── Smooth Rotation ──────────────────────────────────
        // Only rotate when there is meaningful movement to avoid jitter.
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation  = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // ── Translation ──────────────────────────────────────
        // Move forward in world space using the calculated direction.
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Checks if the car is close enough to the current waypoint.
    /// If so, advances to the next waypoint (looping at the end).
    /// </summary>
    private void CheckWaypointReached()
    {
        Transform target       = waypoints[_currentWaypointIndex];
        Vector3   targetFlat   = GetFlatPosition(target.position);
        Vector3   myFlat       = GetFlatPosition(transform.position);

        float distanceToTarget = Vector3.Distance(myFlat, targetFlat);

        if (distanceToTarget <= waypointReachDistance)
        {
            AdvanceToNextWaypoint();
        }
    }

    /// <summary>
    /// Moves the waypoint index forward by one.
    /// Wraps back to index 0 after the last waypoint (looping route).
    /// </summary>
    private void AdvanceToNextWaypoint()
    {
        _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
    }

    // ──────────────────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────────────────

    /// <summary>
    /// Strips the Y component so direction calculations stay flat.
    /// This prevents the car from tilting up/down toward waypoints
    /// that may be at slightly different heights.
    /// </summary>
    private Vector3 GetFlatPosition(Vector3 position)
    {
        return new Vector3(position.x, transform.position.y, position.z);
    }

    /// <summary>
    /// Immediately snaps the car's rotation to face a waypoint.
    /// Called once on Start to avoid a spin-up on the first frame.
    /// </summary>
    private void FaceWaypointInstant(Transform target)
    {
        Vector3 direction = GetFlatPosition(target.position) - GetFlatPosition(transform.position);
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    /// <summary>
    /// Runs safety checks before allowing movement.
    /// Logs clear, beginner-friendly error messages in the Console.
    /// </summary>
    private bool ValidateWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"[CarAI] '{gameObject.name}': No waypoints assigned! " +
                           "Please add at least 2 waypoints in the Inspector.", this);
            return false;
        }

        if (waypoints.Length < 2)
        {
            Debug.LogWarning($"[CarAI] '{gameObject.name}': Only 1 waypoint found. " +
                             "Add more waypoints for the car to follow a real path.", this);
        }

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                Debug.LogError($"[CarAI] '{gameObject.name}': Waypoint slot [{i}] is empty (null). " +
                               "Make sure every waypoint slot is filled in the Inspector.", this);
                return false;
            }
        }

        return true;
    }

    // ──────────────────────────────────────────────────────────
    //  GIZMOS  (visible in Scene View, editor only)
    // ──────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!showDebugPath) return;
        if (waypoints == null || waypoints.Length == 0) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // Draw a sphere at each waypoint position
            Gizmos.color = (i == _currentWaypointIndex) ? Color.green : Color.yellow;
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);

            // Draw a line from this waypoint to the next (wrap at end)
            int nextIndex = (i + 1) % waypoints.Length;
            if (waypoints[nextIndex] != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
            }
        }

        // Draw a line from the car to its current target waypoint
        if (Application.isPlaying && waypoints[_currentWaypointIndex] != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, waypoints[_currentWaypointIndex].position);
        }
    }
}