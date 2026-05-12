using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianWaypoint : MonoBehaviour
    {
        [SerializeField] private bool waitAtWaypoint;
        [SerializeField] private float minWaitTime = 0f;
        [SerializeField] private float maxWaitTime = 1.2f;
        [SerializeField] private PedestrianCrossingZone crossingZone;

        public bool WaitAtWaypoint => waitAtWaypoint;
        public float MinWaitTime => Mathf.Max(0f, minWaitTime);
        public float MaxWaitTime => Mathf.Max(MinWaitTime, maxWaitTime);
        public PedestrianCrossingZone CrossingZone => crossingZone;
    }
}
