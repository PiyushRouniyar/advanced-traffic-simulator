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

        [SerializeField] private float redTime = 8f;
        [SerializeField] private float yellowTime = 2f;
        [SerializeField] private float greenTime = 8f;
        [SerializeField] private TrafficLightState startState = TrafficLightState.Red;
        [SerializeField] private bool stopOnYellow = true;

        public TrafficLightState CurrentState { get; private set; }
        public bool ShouldStopCars => CurrentState == TrafficLightState.Red || (stopOnYellow && CurrentState == TrafficLightState.Yellow);
        public bool ShouldStopCarsNow() => ShouldStopCars;

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

        public void SetRed() => SetState(TrafficLightState.Red);
        public void SetYellow() => SetState(TrafficLightState.Yellow);
        public void SetGreen() => SetState(TrafficLightState.Green);

        private void CycleState()
        {
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
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = CurrentState == TrafficLightState.Red ? Color.red :
                           CurrentState == TrafficLightState.Yellow ? Color.yellow : Color.green;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.2f);
        }
    }
}
