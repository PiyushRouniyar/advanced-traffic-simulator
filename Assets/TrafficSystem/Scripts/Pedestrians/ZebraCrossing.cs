using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class ZebraCrossing : MonoBehaviour
    {
        [SerializeField] private CrosswalkController crosswalkController;
        [SerializeField] private PedestrianCrossingZone crossingZone;
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color canCrossColor = new Color(0.2f, 1f, 0.3f, 0.25f);
        [SerializeField] private Color stopColor = new Color(1f, 0.2f, 0.2f, 0.25f);
        [SerializeField] private Vector3 gizmoSize = new Vector3(3f, 0.2f, 6f);

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
            {
                return;
            }

            bool canCross = crosswalkController == null || crosswalkController.CanPedestriansCross;
            Gizmos.color = canCross ? canCrossColor : stopColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero + Vector3.up * 0.05f, gizmoSize);
        }

        private void OnValidate()
        {
            if (crosswalkController == null)
            {
                crosswalkController = GetComponent<CrosswalkController>();
            }
            if (crossingZone == null)
            {
                crossingZone = GetComponent<PedestrianCrossingZone>();
            }
        }
    }
}
