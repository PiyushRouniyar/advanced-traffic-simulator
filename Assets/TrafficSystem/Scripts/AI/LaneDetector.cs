using MyTrafficSystem.Vehicles;
using MyTrafficSystem.Waypoints;
using UnityEngine;

namespace MyTrafficSystem.AI
{
    /// <summary>
    /// Handles lane and obstacle sensing used by lane-change AI.
    /// </summary>
    public class LaneDetector : MonoBehaviour
    {
        [Header("Lane References")]
        [SerializeField] private WaypointPath leftLanePath;
        [SerializeField] private WaypointPath rightLanePath;

        [Header("Detection")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float laneCheckRadius = 1.8f;
        [SerializeField] private LayerMask vehicleLayerMask = ~0;

        [Header("Ray Origin")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float rayHeightOffset = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;

        public WaypointPath LeftLanePath => leftLanePath;
        public WaypointPath RightLanePath => rightLanePath;
        public float DetectionRange => Mathf.Max(0.1f, detectionRange);

        public void SetDetectionRange(float range)
        {
            detectionRange = Mathf.Max(0.1f, range);
        }

        public bool HasVehicleAhead()
        {
            Vector3 start = GetRayStart();
            bool hit = Physics.Raycast(start, transform.forward, out RaycastHit rayHit, DetectionRange, vehicleLayerMask, QueryTriggerInteraction.Ignore);

            if (drawDebug)
            {
                Color color = hit ? Color.red : Color.green;
                float distance = hit ? rayHit.distance : DetectionRange;
                Debug.DrawRay(start, transform.forward * distance, color);
            }

            if (!hit)
            {
                return false;
            }

            return rayHit.collider.transform.root != transform.root;
        }

        public bool IsLaneSafe(WaypointPath lanePath)
        {
            if (lanePath == null)
            {
                return false;
            }

            int closestIndex = lanePath.GetClosestWaypointIndex(transform.position);
            if (closestIndex < 0)
            {
                return false;
            }

            Waypoint candidate = lanePath.GetWaypoint(closestIndex);
            if (candidate == null)
            {
                return false;
            }

            Vector3 samplePosition = candidate.Position + Vector3.up * rayHeightOffset;
            Collider[] blockers = Physics.OverlapSphere(samplePosition, laneCheckRadius, vehicleLayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < blockers.Length; i++)
            {
                if (blockers[i] != null && blockers[i].transform.root != transform.root)
                {
                    return false;
                }
            }

            if (drawDebug)
            {
                Debug.DrawRay(samplePosition, transform.forward * 1.5f, Color.cyan);
            }

            return true;
        }

        private Vector3 GetRayStart()
        {
            Vector3 start = rayOrigin != null ? rayOrigin.position : transform.position;
            start.y += rayHeightOffset;
            return start;
        }
    }
}
