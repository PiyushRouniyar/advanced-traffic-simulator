using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    [CreateAssetMenu(fileName = "VehicleConfiguration", menuName = "Traffic System/Vehicles/Vehicle Configuration")]
    public class VehicleConfiguration : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string vehicleTypeName = "Sedan";

        [Header("Movement")]
        [SerializeField] private float maxSpeed = 12f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float braking = 12f;
        [SerializeField] private float turnSpeed = 6f;

        [Header("Waypoint Behavior")]
        [SerializeField] private float stoppingDistance = 1.5f;

        public string VehicleTypeName => vehicleTypeName;
        public float MaxSpeed => Mathf.Max(0f, maxSpeed);
        public float Acceleration => Mathf.Max(0f, acceleration);
        public float Braking => Mathf.Max(0f, braking);
        public float TurnSpeed => Mathf.Max(0f, turnSpeed);
        public float StoppingDistance => Mathf.Max(0.1f, stoppingDistance);
    }
}
