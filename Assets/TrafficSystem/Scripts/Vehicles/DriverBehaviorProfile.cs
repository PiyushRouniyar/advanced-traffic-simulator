using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Applies randomized driver personality values to a car at runtime.
    /// </summary>
    public class DriverBehaviorProfile : MonoBehaviour
    {
        [Header("Population Mix")]
        [SerializeField] [Range(0f, 1f)] private float aggressiveDriverPercent = 0.3f;

        [Header("Speed Variation")]
        [SerializeField] private Vector2 speedVariationRange = new Vector2(0.9f, 1.15f);

        [Header("Reaction")]
        [SerializeField] private Vector2 reactionTimeRange = new Vector2(0.15f, 0.75f);

        [Header("Aggressive Multipliers")]
        [SerializeField] private float aggressiveAccelerationMultiplier = 1.2f;
        [SerializeField] private float aggressiveBrakingMultiplier = 1.15f;

        [Header("Calm Multipliers")]
        [SerializeField] private float calmAccelerationMultiplier = 0.9f;
        [SerializeField] private float calmBrakingMultiplier = 0.95f;

        [Header("Debug")]
        [SerializeField] private bool randomizeOnEnable = true;

        private CarWaypointFollower carFollower;

        private void Awake()
        {
            carFollower = GetComponent<CarWaypointFollower>();
        }

        private void OnEnable()
        {
            if (randomizeOnEnable)
            {
                ApplyRandomBehavior();
            }
        }

        [ContextMenu("Apply Random Behavior")]
        public void ApplyRandomBehavior()
        {
            if (carFollower == null)
            {
                carFollower = GetComponent<CarWaypointFollower>();
            }

            if (carFollower == null)
            {
                return;
            }

            bool isAggressive = Random.value <= aggressiveDriverPercent;
            float speedMultiplier = Random.Range(
                Mathf.Min(speedVariationRange.x, speedVariationRange.y),
                Mathf.Max(speedVariationRange.x, speedVariationRange.y));

            float reactionTime = Random.Range(
                Mathf.Min(reactionTimeRange.x, reactionTimeRange.y),
                Mathf.Max(reactionTimeRange.x, reactionTimeRange.y));

            float accelerationMultiplier = isAggressive ? aggressiveAccelerationMultiplier : calmAccelerationMultiplier;
            float brakingMultiplier = isAggressive ? aggressiveBrakingMultiplier : calmBrakingMultiplier;

            carFollower.ApplyDriverBehavior(speedMultiplier, accelerationMultiplier, brakingMultiplier, reactionTime);
        }
    }
}
