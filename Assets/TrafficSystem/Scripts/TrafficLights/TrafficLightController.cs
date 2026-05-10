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

        public TrafficLightState CurrentState { get; private set; }
        public bool ShouldStopCars => CurrentState == TrafficLightState.Red || (stopOnYellow && CurrentState == TrafficLightState.Yellow);
        public bool ShouldStopCarsNow() => ShouldStopCars;
        public KeyCode KeyboardToggleKey => keyboardToggleKey;

        private float timer;

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
            // Keep player control simple and gameplay-focused: toggle between stop/go.
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
            if (state == TrafficLightState.Red)
            {
                timer = Mathf.Max(0.2f, redTime);
            }
            else if (state == TrafficLightState.Yellow)
            {
                timer = Mathf.Max(0.2f, yellowTime);
            }
            else
            {
                timer = Mathf.Max(0.2f, greenTime);
            }

            ApplyVisuals();
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

        private void OnDrawGizmos()
        {
            Gizmos.color = CurrentState == TrafficLightState.Red ? Color.red :
                           CurrentState == TrafficLightState.Yellow ? Color.yellow : Color.green;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
    }
}
