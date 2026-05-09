using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Blinks turn indicators based on current steering angular velocity.
    /// </summary>
    public class IndicatorController : MonoBehaviour
    {
        [SerializeField] private Rigidbody carRigidbody;
        [SerializeField] private Renderer[] leftIndicatorRenderers;
        [SerializeField] private Renderer[] rightIndicatorRenderers;
        [SerializeField] private Light[] leftIndicatorLights;
        [SerializeField] private Light[] rightIndicatorLights;

        [Header("Blink Settings")]
        [SerializeField] private float blinkInterval = 0.35f;
        [SerializeField] private float turnDetectAngularVelocity = 0.15f;
        [SerializeField] private Color indicatorOnColor = new Color(1f, 0.45f, 0f, 1f);
        [SerializeField] private Color indicatorOffColor = new Color(0.12f, 0.05f, 0f, 1f);

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private float blinkTimer;
        private bool blinkState;

        private void Awake()
        {
            if (carRigidbody == null)
            {
                carRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = Mathf.Max(0.1f, blinkInterval);
                blinkState = !blinkState;
            }

            float yawVelocity = carRigidbody != null ? carRigidbody.angularVelocity.y : 0f;
            bool leftActive = yawVelocity < -turnDetectAngularVelocity;
            bool rightActive = yawVelocity > turnDetectAngularVelocity;

            ApplyIndicatorSet(leftIndicatorRenderers, leftIndicatorLights, leftActive && blinkState);
            ApplyIndicatorSet(rightIndicatorRenderers, rightIndicatorLights, rightActive && blinkState);
        }

        private void ApplyIndicatorSet(Renderer[] renderers, Light[] lights, bool enabledState)
        {
            Color targetColor = enabledState ? indicatorOnColor : indicatorOffColor;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rendererTarget = renderers[i];
                if (rendererTarget == null)
                {
                    continue;
                }

                Material material = rendererTarget.material;
                material.SetColor(EmissionColor, targetColor);
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].enabled = enabledState;
                }
            }
        }
    }
}
