using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Handles renderer culling for far-away vehicles.
    /// </summary>
    public class DistanceCullingSystem : MonoBehaviour
    {
        [SerializeField] private float cullDistance = 180f;
        [SerializeField] private Renderer[] renderersToCull;

        private bool isCulled;

        public bool IsCulled => isCulled;
        public float CullDistance => Mathf.Max(10f, cullDistance);

        public void ApplyCulling(float distanceToViewer)
        {
            bool shouldCull = distanceToViewer > CullDistance;
            if (shouldCull == isCulled)
            {
                return;
            }

            isCulled = shouldCull;
            for (int i = 0; i < renderersToCull.Length; i++)
            {
                if (renderersToCull[i] != null)
                {
                    renderersToCull[i].enabled = !isCulled;
                }
            }
        }
    }
}
