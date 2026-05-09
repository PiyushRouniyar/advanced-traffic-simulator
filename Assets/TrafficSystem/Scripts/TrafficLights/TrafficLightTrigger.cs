using System.Collections.Generic;
using MyTrafficSystem.Vehicles;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    /// <summary>
    /// Trigger zone that tells cars to stop or move based on linked traffic light state.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrafficLightTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrafficLightController trafficLightController;

        [Header("Gizmos")]
        [SerializeField] private bool drawTriggerGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0.1f, 0.35f);

        private readonly HashSet<CarWaypointFollower> carsInTrigger = new HashSet<CarWaypointFollower>();
        private Collider cachedTriggerCollider;
        private bool lastAppliedShouldStop;

        private void Awake()
        {
            cachedTriggerCollider = GetComponent<Collider>();
            cachedTriggerCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            if (trafficLightController == null)
            {
                trafficLightController = GetComponentInParent<TrafficLightController>();
            }

            lastAppliedShouldStop = ShouldStopCarsNow();
        }

        private void OnTriggerEnter(Collider other)
        {
            CarWaypointFollower car = other.GetComponentInParent<CarWaypointFollower>();
            if (car == null || carsInTrigger.Contains(car))
            {
                return;
            }

            carsInTrigger.Add(car);
            if (lastAppliedShouldStop)
            {
                car.SetTrafficLightStop(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            CarWaypointFollower car = other.GetComponentInParent<CarWaypointFollower>();
            if (car == null || !carsInTrigger.Contains(car))
            {
                return;
            }

            carsInTrigger.Remove(car);
            if (lastAppliedShouldStop)
            {
                car.SetTrafficLightStop(false);
            }
        }

        private void Update()
        {
            if (carsInTrigger.Count == 0)
            {
                return;
            }

            bool shouldStop = ShouldStopCarsNow();
            if (shouldStop == lastAppliedShouldStop)
            {
                return;
            }

            foreach (CarWaypointFollower car in carsInTrigger)
            {
                if (car == null)
                {
                    continue;
                }

                if (lastAppliedShouldStop)
                {
                    car.SetTrafficLightStop(false);
                }

                if (shouldStop)
                {
                    car.SetTrafficLightStop(true);
                }
            }

            lastAppliedShouldStop = shouldStop;
        }

        private bool ShouldStopCarsNow()
        {
            return trafficLightController != null && trafficLightController.ShouldStopCars;
        }

        private void OnDisable()
        {
            foreach (CarWaypointFollower car in carsInTrigger)
            {
                if (car == null)
                {
                    continue;
                }

                car.SetTrafficLightStop(false);
            }

            carsInTrigger.Clear();
        }

        private void OnDrawGizmos()
        {
            if (!drawTriggerGizmo)
            {
                return;
            }

            Collider trigger = GetComponent<Collider>();
            if (trigger == null)
            {
                return;
            }

            Gizmos.color = gizmoColor;

            BoxCollider box = trigger as BoxCollider;
            if (box != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
