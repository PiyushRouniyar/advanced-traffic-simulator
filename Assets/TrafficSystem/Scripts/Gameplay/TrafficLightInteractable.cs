using UnityEngine;
using MyTrafficSystem.AI;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class TrafficLightInteractable : MonoBehaviour
    {
        [SerializeField] private MyTrafficSystem.TrafficLights.TrafficLightController trafficLight;
        [SerializeField] private string displayName = "Traffic Light";
        [SerializeField] private float waitingCheckRadius = 18f;
        [SerializeField] private Renderer[] highlightRenderers;
        [SerializeField] private Color highlightColor = new Color(0.35f, 0.9f, 1f, 1f);
        [SerializeField] private float highlightIntensity = 1.5f;

        private bool highlighted;

        public MyTrafficSystem.TrafficLights.TrafficLightController Light => trafficLight;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

        private void Reset()
        {
            trafficLight = GetComponent<MyTrafficSystem.TrafficLights.TrafficLightController>();
            Collider c = GetComponent<Collider>();
            c.isTrigger = true;
            if (c is SphereCollider sphere)
            {
                sphere.radius = 7f;
            }

            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Awake()
        {
            if (trafficLight == null)
            {
                trafficLight = GetComponent<MyTrafficSystem.TrafficLights.TrafficLightController>();
            }
        }

        private void Update()
        {
            UpdateHighlight();
        }

        public void TurnGreen() => trafficLight?.SetGreen();
        public void TurnRed() => trafficLight?.SetRed();

        public string CurrentStateText => trafficLight == null ? "Unknown" : trafficLight.CurrentState.ToString();
        public string CurrentModeText => "Manual";

        public int EstimateWaitingCars()
        {
            TrafficCarAI[] cars = FindObjectsByType<TrafficCarAI>(FindObjectsSortMode.None);
            int waiting = 0;
            float sqr = waitingCheckRadius * waitingCheckRadius;
            Vector3 p = transform.position;
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i] == null)
                {
                    continue;
                }

                if ((cars[i].transform.position - p).sqrMagnitude <= sqr)
                {
                    waiting++;
                }
            }
            return waiting;
        }

        public void SetHighlighted(bool on)
        {
            highlighted = on;
        }

        private void UpdateHighlight()
        {
            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                return;
            }

            float pulse = highlighted ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f)) : 0f;
            Color emission = highlightColor * (highlightIntensity * pulse);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                Renderer r = highlightRenderers[i];
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(block);
            }
        }
    }
}
