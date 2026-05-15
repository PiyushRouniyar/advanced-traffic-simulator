using UnityEngine;

namespace MyTrafficSystem.Gameplay.FreeMode
{
    [DisallowMultipleComponent]
    public class FreeModeSpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool preferred;
        [SerializeField] private float clearanceRadius = 2.5f;

        public bool Preferred => preferred;
        public float ClearanceRadius => Mathf.Max(0.5f, clearanceRadius);

        private void OnDrawGizmos()
        {
            Gizmos.color = preferred ? new Color(0.22f, 1f, 0.42f, 0.9f) : new Color(0.3f, 0.75f, 1f, 0.85f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.22f);
            Gizmos.DrawWireSphere(transform.position, ClearanceRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
