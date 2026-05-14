using System;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    [DisallowMultipleComponent]
    public class TrafficLightController : MonoBehaviour
    {
        [Header("Control")]
        [SerializeField] private bool playerControlled = true;
        [SerializeField] private bool autoCycleWhenNotPlayerControlled;
        [SerializeField] private KeyCode keyboardToggleKey = KeyCode.None;

        [Header("Timing")]
        [SerializeField] private float redTime = 8f;
        [SerializeField] private float yellowTime = 2f;
        [SerializeField] private float greenTime = 8f;
        [SerializeField] private TrafficLightState startState = TrafficLightState.Red;
        [SerializeField] private bool stopOnYellow = true;

        [Header("Visual (Optional)")]
        [SerializeField] private Renderer redRenderer;
        [SerializeField] private Renderer yellowRenderer;
        [SerializeField] private Renderer greenRenderer;
        [SerializeField] private float emissionIntensity = 2.2f;
        [SerializeField] private bool autoDetectLampRenderers = true;

        public TrafficLightState CurrentState { get; private set; }
        public bool ShouldStopCars => CurrentState == TrafficLightState.Red || (stopOnYellow && CurrentState == TrafficLightState.Yellow);
        public bool ShouldStopCarsNow() => ShouldStopCars;
        public KeyCode KeyboardToggleKey => keyboardToggleKey;
        public bool IsPlayerControlled => playerControlled;
        public bool AutoCycleEnabled => autoCycleWhenNotPlayerControlled;
        public float RemainingTimer => Mathf.Max(0f, timer);
        public event Action<TrafficLightState> StateChanged;

        private float timer;

        private void Awake()
        {
            if (autoDetectLampRenderers)
            {
                TryAutoAssignRenderers();
            }
        }

        private void Start()
        {
            SetState(startState);
        }

        private void Update()
        {
            if (playerControlled)
            {
                if (keyboardToggleKey != KeyCode.None && Input.GetKeyDown(keyboardToggleKey))
                {
                    CycleState();
                }
                return;
            }

            if (!autoCycleWhenNotPlayerControlled)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            CycleState();
        }

        private void OnMouseDown()
        {
            if (!playerControlled)
            {
                return;
            }

            CycleState();
        }

        public void ForceState(TrafficLightState state)
        {
            SetState(state);
        }

        public void SetKeyboardToggleKey(KeyCode key)
        {
            keyboardToggleKey = key;
            playerControlled = key != KeyCode.None;
        }

        public void SetRed() => SetState(TrafficLightState.Red);
        public void SetYellow() => SetState(TrafficLightState.Yellow);
        public void SetGreen() => SetState(TrafficLightState.Green);

        private void CycleState()
        {
            if (playerControlled)
            {
                SetState(CurrentState == TrafficLightState.Red ? TrafficLightState.Green : TrafficLightState.Red);
                return;
            }

            if (CurrentState == TrafficLightState.Red)
            {
                SetState(TrafficLightState.Green);
            }
            else if (CurrentState == TrafficLightState.Green)
            {
                SetState(TrafficLightState.Yellow);
            }
            else
            {
                SetState(TrafficLightState.Red);
            }
        }

        private void SetState(TrafficLightState state)
        {
            CurrentState = state;
            if (state == TrafficLightState.Red) timer = Mathf.Max(0.2f, redTime);
            else if (state == TrafficLightState.Yellow) timer = Mathf.Max(0.2f, yellowTime);
            else timer = Mathf.Max(0.2f, greenTime);

            ApplyVisuals();
            StateChanged?.Invoke(state);
        }

        private void ApplyVisuals()
        {
            SetLamp(redRenderer, CurrentState == TrafficLightState.Red ? Color.red : Color.black);
            SetLamp(yellowRenderer, CurrentState == TrafficLightState.Yellow ? Color.yellow : Color.black);
            SetLamp(greenRenderer, CurrentState == TrafficLightState.Green ? Color.green : Color.black);
        }

        private void SetLamp(Renderer rendererTarget, Color color)
        {
            if (rendererTarget == null)
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rendererTarget.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", color * emissionIntensity);
            rendererTarget.SetPropertyBlock(block);
        }

        private void TryAutoAssignRenderers()
        {
            if (redRenderer != null && yellowRenderer != null && greenRenderer != null)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                string n = renderers[i].name.ToLowerInvariant();
                if (redRenderer == null && n.Contains("red")) redRenderer = renderers[i];
                else if (yellowRenderer == null && (n.Contains("yellow") || n.Contains("amber"))) yellowRenderer = renderers[i];
                else if (greenRenderer == null && n.Contains("green")) greenRenderer = renderers[i];
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = CurrentState == TrafficLightState.Red ? Color.red :
                           CurrentState == TrafficLightState.Yellow ? Color.yellow : Color.green;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
    }
}
