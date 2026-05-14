using MyTrafficSystem.TrafficLights;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenCrossingNode : MonoBehaviour
    {
        [SerializeField] private TrafficLightGroup linkedTrafficGroup;
        [SerializeField] private bool invertCarsGreen = true;
        [SerializeField] private float triggerRadius = 1.4f;
        [SerializeField] private float safetyCheckRadius = 7f;
        [SerializeField] private float unsafeVehicleRadius = 3.25f;
        [SerializeField] private float approachingDotThreshold = 0.45f;
        [SerializeField] private LayerMask vehicleMask = ~0;
        [SerializeField] private bool drawVehicleDebug = true;

        private static readonly Collider[] VehicleHits = new Collider[48];
        private int lastDetectedVehicleCount;

        public TrafficLightGroup LinkedTrafficGroup => linkedTrafficGroup;
        public float TriggerRadius => Mathf.Max(0.2f, triggerRadius);
        public int LastDetectedVehicleCount => lastDetectedVehicleCount;

        public void AssignGroup(TrafficLightGroup group)
        {
            linkedTrafficGroup = group;
        }

        public bool CanCitizensCross
        {
            get
            {
                if (linkedTrafficGroup == null) return true;
                bool carsGreen = linkedTrafficGroup.IsGreen;
                return invertCarsGreen ? !carsGreen : carsGreen;
            }
        }

        public bool IsCrossingSafe(Vector3 citizenPosition)
        {
            if (!CanCitizensCross)
            {
                lastDetectedVehicleCount = 0;
                return false;
            }

            float detectRadius = Mathf.Max(unsafeVehicleRadius, safetyCheckRadius);
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                detectRadius,
                VehicleHits,
                vehicleMask,
                QueryTriggerInteraction.Ignore);

            int dangerous = 0;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = VehicleHits[i];
                if (hit == null)
                {
                    continue;
                }

                Rigidbody rb = hit.attachedRigidbody;
                if (rb == null)
                {
                    continue;
                }

                // Cars are identified by TrafficCarAI, keeping checks lightweight and explicit.
                if (rb.GetComponent<MyTrafficSystem.AI.TrafficCarAI>() == null &&
                    rb.GetComponentInParent<MyTrafficSystem.AI.TrafficCarAI>() == null)
                {
                    continue;
                }

                Vector3 carPos = rb.worldCenterOfMass;
                Vector3 toCrossing = transform.position - carPos;
                toCrossing.y = 0f;
                float sqrDistance = toCrossing.sqrMagnitude;
                if (sqrDistance > detectRadius * detectRadius)
                {
                    continue;
                }

                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0f;
                float speed = velocity.magnitude;
                if (speed < 0.15f)
                {
                    continue;
                }

                bool veryClose = sqrDistance <= unsafeVehicleRadius * unsafeVehicleRadius;
                bool approachingCrossing = Vector3.Dot(velocity.normalized, toCrossing.normalized) >= approachingDotThreshold;
                bool approachingCitizen = Vector3.Dot(velocity.normalized, (citizenPosition - carPos).normalized) > 0.25f;

                if (veryClose || (approachingCrossing && approachingCitizen))
                {
                    dangerous++;
                }
            }

            lastDetectedVehicleCount = dangerous;
            return dangerous == 0;
        }

        private void OnDrawGizmos()
        {
            if (!CitizenDebugSettings.ShowDebug) return;
            bool safeNow = Application.isPlaying ? (CanCitizensCross && lastDetectedVehicleCount == 0) : CanCitizensCross;
            Gizmos.color = safeNow ? new Color(0.35f, 1f, 0.55f, 1f) : new Color(1f, 0.35f, 0.35f, 1f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.15f, 0.16f);
            Gizmos.DrawWireSphere(transform.position, TriggerRadius);
            if (drawVehicleDebug)
            {
                Gizmos.color = new Color(1f, 0.65f, 0.2f, 0.8f);
                Gizmos.DrawWireSphere(transform.position, Mathf.Max(unsafeVehicleRadius, safetyCheckRadius));
            }
        }
    }
}
