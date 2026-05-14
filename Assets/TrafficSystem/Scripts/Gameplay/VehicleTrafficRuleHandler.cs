using UnityEngine;
namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MyTrafficSystem.AI.TrafficCarAI))]
    public class VehicleTrafficRuleHandler : MonoBehaviour
    {
        [SerializeField] private float brakingMultiplier = 0.75f;
        [SerializeField] private float fullStopThreshold = 0.25f;

        private Rigidbody rb;
        private MyTrafficSystem.AI.TrafficCarAI carAI;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            carAI = GetComponent<MyTrafficSystem.AI.TrafficCarAI>();
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                return;
            }

            if (ShouldForceStopForAssignedLaneSignal())
            {
                Vector3 v = rb.linearVelocity;
                v.x *= Mathf.Clamp01(brakingMultiplier);
                v.z *= Mathf.Clamp01(brakingMultiplier);
                if (v.magnitude < Mathf.Max(0.01f, fullStopThreshold))
                {
                    v.x = 0f;
                    v.z = 0f;
                }
                rb.linearVelocity = v;
            }
        }

        private bool ShouldForceStopForAssignedLaneSignal()
        {
            if (carAI == null || carAI.CurrentLane == null)
            {
                return false;
            }
            return carAI.CurrentLane.ShouldStopAtLight(carAI.CurrentWaypointIndex);
        }
    }
}
