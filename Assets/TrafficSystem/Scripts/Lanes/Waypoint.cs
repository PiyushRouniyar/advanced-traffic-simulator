using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    [DisallowMultipleComponent]
    public class Waypoint : MonoBehaviour
    {
        [SerializeField] private Lane owner;
        [SerializeField] private int index;

        public Lane Owner => owner;
        public int Index => index;

        public void SetOwner(Lane lane, int waypointIndex)
        {
            owner = lane;
            index = waypointIndex;
            gameObject.name = $"WP_{waypointIndex:00}";
        }

        private void OnDrawGizmos()
        {
            if (!TrafficDebugSettings.ShowTrafficDebug) { return; }
            Color c = Color.white;
            if (owner != null)
            {
                if (index == 0)
                {
                    c = Color.green;
                }
                else if (owner.Waypoints.Count > 0 && index == owner.Waypoints.Count - 1)
                {
                    c = Color.red;
                }
            }

            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}
