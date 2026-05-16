using UnityEngine;

namespace MyTrafficSystem.Gameplay.Challenge
{
    [DisallowMultipleComponent]
    public class TrafficDetectableVehicle : MonoBehaviour
    {
        [SerializeField] private Rigidbody targetRigidbody;
        [SerializeField] private Collider[] detectionColliders;

        public Rigidbody TargetRigidbody
        {
            get
            {
                if (targetRigidbody == null)
                {
                    targetRigidbody = GetComponentInParent<Rigidbody>();
                }
                return targetRigidbody;
            }
        }

        public Vector3 DetectionPosition
        {
            get
            {
                Rigidbody rb = TargetRigidbody;
                if (rb != null) return rb.worldCenterOfMass;

                Collider col = FirstCollider;
                if (col != null) return col.bounds.center;

                return transform.position;
            }
        }

        public float SpeedMps
        {
            get
            {
                Rigidbody rb = TargetRigidbody;
                if (rb != null) return rb.linearVelocity.magnitude;
                return 0f;
            }
        }

        private Collider FirstCollider
        {
            get
            {
                if (detectionColliders != null)
                {
                    for (int i = 0; i < detectionColliders.Length; i++)
                    {
                        if (detectionColliders[i] != null) return detectionColliders[i];
                    }
                }

                return GetComponentInChildren<Collider>();
            }
        }

        private void Reset()
        {
            if (targetRigidbody == null) targetRigidbody = GetComponentInParent<Rigidbody>();
            if (detectionColliders == null || detectionColliders.Length == 0)
            {
                detectionColliders = GetComponentsInChildren<Collider>(includeInactive: false);
            }
        }
    }
}
