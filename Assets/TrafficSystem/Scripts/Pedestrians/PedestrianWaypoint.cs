using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianWaypoint : MonoBehaviour
    {
        [Header("Wait")]
        [SerializeField] private bool waitAtWaypoint;
        [SerializeField] private float minWaitTime;
        [SerializeField] private float maxWaitTime = 1.2f;

        [Header("Crosswalk")]
        [SerializeField] private PedestrianCrosswalkNode crosswalkNode;
        [SerializeField] private PedestrianCrossingZone crossingZone;

        public bool WaitAtWaypoint => waitAtWaypoint;
        public float MinWaitTime => Mathf.Max(0f, minWaitTime);
        public float MaxWaitTime => Mathf.Max(MinWaitTime, maxWaitTime);
        public PedestrianCrosswalkNode CrosswalkNode => crosswalkNode;
        // Legacy compatibility for older scripts.
        public PedestrianCrossingZone CrossingZone => crossingZone;

        public bool RequiresCrosswalkCheck => crosswalkNode != null;
    }
}
