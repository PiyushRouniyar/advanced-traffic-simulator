using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Detects vehicles ahead and outputs a normalized speed multiplier.
    /// 1 = full speed, 0 = full stop.
    /// </summary>
    public class ObstacleDetection : MonoBehaviour
    {
        [Header("Raycast Setup")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float rayHeightOffset = 0.6f;
        [SerializeField] private LayerMask vehicleLayerMask = ~0;

        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float minimumStoppingDistance = 2.5f;
        [SerializeField] private float brakeForce = 12f;

        [Header("Debug")]
        [SerializeField] private bool showDebugRay = true;
        [SerializeField] private Color clearRayColor = Color.green;
        [SerializeField] private Color blockedRayColor = Color.red;

        public float DetectionRange => Mathf.Max(0.1f, detectionRange);
        public float MinimumStoppingDistance => Mathf.Max(0.1f, minimumStoppingDistance);
        public float BrakeForce => Mathf.Max(0f, brakeForce);

        public bool IsBlocked { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;

        private void FixedUpdate()
        {
            EvaluateObstacle();
        }

        private void EvaluateObstacle()
        {
            Vector3 start = GetRayStartPosition();
            Vector3 direction = transform.forward;

            bool hitFound = Physics.Raycast(
                start,
                direction,
                out RaycastHit hit,
                DetectionRange,
                vehicleLayerMask,
                QueryTriggerInteraction.Ignore);

            IsBlocked = false;
            SpeedMultiplier = 1f;

            if (!hitFound)
            {
                DrawDebug(start, direction, DetectionRange, clearRayColor);
                return;
            }

            Transform hitRoot = hit.collider.transform.root;
            if (hitRoot == transform.root)
            {
                DrawDebug(start, direction, DetectionRange, clearRayColor);
                return;
            }

            IsBlocked = true;

            if (hit.distance <= MinimumStoppingDistance)
            {
                SpeedMultiplier = 0f;
            }
            else
            {
                float usableRange = Mathf.Max(0.01f, DetectionRange - MinimumStoppingDistance);
                float distancePastStop = hit.distance - MinimumStoppingDistance;
                SpeedMultiplier = Mathf.Clamp01(distancePastStop / usableRange);
            }

            DrawDebug(start, direction, hit.distance, blockedRayColor);
        }

        private Vector3 GetRayStartPosition()
        {
            Vector3 basePosition = rayOrigin != null ? rayOrigin.position : transform.position;
            basePosition.y += rayHeightOffset;
            return basePosition;
        }

        private void DrawDebug(Vector3 start, Vector3 direction, float distance, Color rayColor)
        {
            if (!showDebugRay)
            {
                return;
            }

            Debug.DrawRay(start, direction * distance, rayColor);
        }
    }
}
