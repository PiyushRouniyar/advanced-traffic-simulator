using UnityEngine;

namespace MyTrafficSystem.AI
{
    [DisallowMultipleComponent]
    public class CarSensor : MonoBehaviour
    {
        [SerializeField] private Transform sensorOrigin;
        [SerializeField] private float detectionDistance = 14f;
        [SerializeField] private LayerMask detectionMask = ~0;
        [SerializeField] private bool drawDebugRays = true;

        public bool HasObstacle { get; private set; }
        public float ObstacleDistance { get; private set; }
        public float DetectionDistance => detectionDistance;

        private void Awake()
        {
            if (sensorOrigin == null) { sensorOrigin = transform; }
        }

        private void Update()
        {
            Vector3 origin = sensorOrigin.position + Vector3.up * 0.4f;
            Vector3 direction = sensorOrigin.forward;
            HasObstacle = Physics.Raycast(origin, direction, out RaycastHit hit, detectionDistance, detectionMask, QueryTriggerInteraction.Ignore);
            ObstacleDistance = HasObstacle ? hit.distance : detectionDistance;

            if (drawDebugRays)
            {
                Debug.DrawRay(origin, direction * detectionDistance, HasObstacle ? Color.red : Color.green);
            }
        }
    }
}
