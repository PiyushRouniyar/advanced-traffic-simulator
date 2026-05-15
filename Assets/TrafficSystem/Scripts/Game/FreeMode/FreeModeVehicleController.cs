using UnityEngine;

namespace MyTrafficSystem.Gameplay.FreeMode
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class FreeModeVehicleController : MonoBehaviour
    {
        [SerializeField] private float acceleration = 22f;
        [SerializeField] private float reverseAcceleration = 14f;
        [SerializeField] private float brakingForce = 28f;
        [SerializeField] private float maxSpeed = 24f;
        [SerializeField] private float steerStrength = 85f;
        [SerializeField] private float dragWhenIdle = 1.8f;

        private Rigidbody rb;
        private float throttle;
        private float steer;

        public float SpeedMps => rb != null ? rb.linearVelocity.magnitude : 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = Mathf.Max(900f, rb.mass <= 1f ? 1200f : rb.mass);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.centerOfMass += Vector3.down * 0.45f;
        }

        private void Update()
        {
            throttle = Input.GetAxisRaw("Vertical");
            steer = Input.GetAxisRaw("Horizontal");
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            Vector3 vel = rb.linearVelocity;
            Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
            float currentForwardSpeed = Vector3.Dot(flatVel, transform.forward);

            float targetAccel = throttle >= 0f ? acceleration : reverseAcceleration;
            Vector3 drive = transform.forward * (throttle * targetAccel);
            rb.AddForce(drive, ForceMode.Acceleration);

            if (Mathf.Approximately(throttle, 0f))
            {
                rb.linearVelocity = new Vector3(
                    Mathf.Lerp(vel.x, 0f, dragWhenIdle * Time.fixedDeltaTime),
                    vel.y,
                    Mathf.Lerp(vel.z, 0f, dragWhenIdle * Time.fixedDeltaTime));
            }

            if ((throttle > 0f && currentForwardSpeed < 0f) || (throttle < 0f && currentForwardSpeed > 0f))
            {
                rb.AddForce(-flatVel.normalized * brakingForce, ForceMode.Acceleration);
            }

            float steerScale = Mathf.Clamp01(1f - (flatVel.magnitude / Mathf.Max(1f, maxSpeed)) * 0.5f);
            float yaw = steer * steerStrength * steerScale * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));

            Vector3 clamped = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (clamped.magnitude > maxSpeed)
            {
                clamped = clamped.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }
        }
    }
}
