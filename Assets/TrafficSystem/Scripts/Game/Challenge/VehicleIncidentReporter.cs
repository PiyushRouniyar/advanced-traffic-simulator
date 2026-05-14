using UnityEngine;
using MyTrafficSystem.AI;
using MyTrafficSystem.Lanes;

namespace MyTrafficSystem.Gameplay.Challenge
{
    [DisallowMultipleComponent]
    public class VehicleIncidentReporter : MonoBehaviour
    {
        [SerializeField] private float minImpactMagnitude = 1.2f;
        [SerializeField] private float incidentCooldown = 1.2f;

        private float cooldownTimer;
        private TrafficCarAI carAI;
        private Lane cachedLane;

        private void Awake()
        {
            carAI = GetComponent<TrafficCarAI>();
        }

        private void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (cooldownTimer > 0f || collision == null) return;
            if (collision.relativeVelocity.magnitude < minImpactMagnitude) return;

            CameraChallengeManager manager = CameraChallengeManager.Instance;
            if (manager != null)
            {
                cachedLane = carAI != null ? carAI.CurrentLane : null;
                manager.ReportIncident(transform.position, cachedLane);
                cooldownTimer = incidentCooldown;
            }
        }
    }
}
