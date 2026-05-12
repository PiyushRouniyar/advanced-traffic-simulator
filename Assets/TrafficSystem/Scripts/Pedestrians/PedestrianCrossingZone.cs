using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianCrossingZone : MonoBehaviour
    {
        [SerializeField] private PedestrianCrossingController controller;
        [SerializeField] private Collider zoneCollider;
        [SerializeField] private bool drawDebug = true;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 0f, 0.2f);
        [SerializeField] private Color activeColor = new Color(1f, 0.4f, 0.2f, 0.3f);

        private readonly HashSet<PedestrianAI> crossingPedestrians = new HashSet<PedestrianAI>();
        private static readonly List<PedestrianCrossingZone> AllZones = new List<PedestrianCrossingZone>();

        public bool CanPedestriansCross => controller == null || controller.CanPedestriansCross;
        public bool HasActiveCrossingPedestrians => crossingPedestrians.Count > 0;

        private void OnEnable()
        {
            if (!AllZones.Contains(this))
            {
                AllZones.Add(this);
            }
            ResolveCollider();
        }

        private void OnDisable()
        {
            AllZones.Remove(this);
            crossingPedestrians.Clear();
        }

        public void EnterCrossing(PedestrianAI pedestrian)
        {
            if (pedestrian != null)
            {
                crossingPedestrians.Add(pedestrian);
            }
        }

        public void ExitCrossing(PedestrianAI pedestrian)
        {
            if (pedestrian != null)
            {
                crossingPedestrians.Remove(pedestrian);
            }
        }

        public static bool IsCrosswalkBlockingCars(Vector3 origin, Vector3 forward, float checkDistance)
        {
            Vector3 forwardFlat = forward;
            forwardFlat.y = 0f;
            if (forwardFlat.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            forwardFlat.Normalize();
            float maxDistance = Mathf.Max(1f, checkDistance);

            for (int i = 0; i < AllZones.Count; i++)
            {
                PedestrianCrossingZone zone = AllZones[i];
                if (zone == null || !zone.HasActiveCrossingPedestrians)
                {
                    continue;
                }

                Vector3 toZone = zone.transform.position - origin;
                toZone.y = 0f;
                float dot = Vector3.Dot(forwardFlat, toZone.normalized);
                if (dot < 0.4f)
                {
                    continue;
                }

                float sqrDist = toZone.sqrMagnitude;
                if (sqrDist <= maxDistance * maxDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveCollider()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider>();
            }

            if (controller == null)
            {
                controller = GetComponent<PedestrianCrossingController>();
                if (controller == null)
                {
                    controller = GetComponent<CrosswalkController>();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawDebug)
            {
                return;
            }

            ResolveCollider();
            Gizmos.color = HasActiveCrossingPedestrians ? activeColor : idleColor;
            if (zoneCollider is BoxCollider box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 1.2f);
            }
        }
    }
}
