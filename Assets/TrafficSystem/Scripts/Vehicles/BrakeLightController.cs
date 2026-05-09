using UnityEngine;

namespace MyTrafficSystem.Vehicles
{
    /// <summary>
    /// Simple brake light visual controller.
    /// </summary>
    public class BrakeLightController : MonoBehaviour
    {
        [SerializeField] private CarWaypointFollower carFollower;
        [SerializeField] private Renderer[] brakeLightRenderers;
        [SerializeField] private Light[] brakeLights;
        [SerializeField] private Color brakeOnColor = Color.red;
        [SerializeField] private Color brakeOffColor = new Color(0.15f, 0f, 0f, 1f);

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (carFollower == null)
            {
                carFollower = GetComponent<CarWaypointFollower>();
            }
        }

        private void Update()
        {
            bool braking = carFollower != null && carFollower.IsBraking;
            ApplyBrakeVisual(braking);
        }

        private void ApplyBrakeVisual(bool enabledState)
        {
            Color targetColor = enabledState ? brakeOnColor : brakeOffColor;

            for (int i = 0; i < brakeLightRenderers.Length; i++)
            {
                Renderer rendererTarget = brakeLightRenderers[i];
                if (rendererTarget == null)
                {
                    continue;
                }

                Material material = rendererTarget.material;
                material.SetColor(EmissionColor, targetColor);
            }

            for (int i = 0; i < brakeLights.Length; i++)
            {
                if (brakeLights[i] != null)
                {
                    brakeLights[i].enabled = enabledState;
                }
            }
        }
    }
}
