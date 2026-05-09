using MyTrafficSystem.Waypoints;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    /// <summary>
    /// Defines where a vehicle can spawn and which path it should start on.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Route Setup")]
        [SerializeField] private WaypointPath assignedPath;
        [SerializeField] private int startingWaypointIndex;

        [Header("Spawn Safety")]
        [SerializeField] private float overlapCheckRadius = 1.6f;
        [SerializeField] private LayerMask blockingLayers = ~0;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color freeColor = new Color(0.2f, 1f, 0.3f, 0.6f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        public WaypointPath AssignedPath => assignedPath;
        public int StartingWaypointIndex => Mathf.Max(0, startingWaypointIndex);

        public Vector3 SpawnPosition => transform.position;
        public Quaternion SpawnRotation => transform.rotation;

        public bool CanSpawn()
        {
            Collider[] overlaps = Physics.OverlapSphere(SpawnPosition, overlapCheckRadius, blockingLayers, QueryTriggerInteraction.Ignore);
            return overlaps.Length == 0;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = CanSpawn() ? freeColor : blockedColor;
            Gizmos.DrawSphere(SpawnPosition, overlapCheckRadius);
            Gizmos.DrawRay(SpawnPosition, transform.forward * 2f);
        }
    }
}
