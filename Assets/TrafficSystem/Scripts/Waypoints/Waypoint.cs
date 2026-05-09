using UnityEngine;

namespace MyTrafficSystem.Waypoints
{
    /// <summary>
    /// Simple marker component for waypoint positions.
    /// </summary>
    public class Waypoint : MonoBehaviour
    {
        [SerializeField] private float gizmoRadius = 0.35f;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 1f);

        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, gizmoRadius);
        }
    }
}
