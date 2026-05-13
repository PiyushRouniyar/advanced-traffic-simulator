using System.Collections.Generic;
using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    [DisallowMultipleComponent]
    public class TrafficLightGroup : MonoBehaviour
    {
        private enum AutoPhase { Green, Yellow, Red }

        [Header("Setup")]
        [SerializeField] private string groupName = "Group";
        public KeyCode assignedKey = KeyCode.Alpha1;
        [SerializeField] private bool startGreen = true;
        [SerializeField] private List<Lane> assignedLanes = new List<Lane>();
        [SerializeField] private List<TrafficLightController> controlledLights = new List<TrafficLightController>();
        [SerializeField] private int defaultStopWaypointIndex = 0;

        [Header("Auto Timer")]
        [SerializeField] private bool autoSwitch;
        [SerializeField] private float greenDuration = 20f;
        [SerializeField] private float yellowDuration = 3f;
        [SerializeField] private float redDuration = 20f;
        [SerializeField] private TrafficLightGroup oppositeGroup;
        [SerializeField] private bool showDebugCountdown = true;

        public bool IsGreen { get; private set; } = true;
        public string GroupName => groupName;
        public KeyCode ActivationKey => assignedKey;
        public float RemainingTime => Mathf.Max(0f, phaseTimer);
        public TrafficLightState DebugState => phase == AutoPhase.Green ? TrafficLightState.Green :
                                               phase == AutoPhase.Yellow ? TrafficLightState.Yellow :
                                               TrafficLightState.Red;

        private AutoPhase phase;
        private float phaseTimer;

        private void Awake()
        {
            if (startGreen)
            {
                SetPhase(AutoPhase.Green);
            }
            else
            {
                SetPhase(AutoPhase.Red);
            }
            AssignStateToLanes();
        }

        private void Update()
        {
            if (assignedKey != KeyCode.None && Input.GetKeyDown(assignedKey))
            {
                ToggleGroupState();
            }

            if (autoSwitch)
            {
                TickAutoTimer();
            }
        }

        public void SetActivationKey(KeyCode key)
        {
            assignedKey = key;
        }

        public bool MatchesKeyDown()
        {
            return assignedKey != KeyCode.None && Input.GetKeyDown(assignedKey);
        }

        public void ToggleGroupState()
        {
            SetGreen(!IsGreen);
            AssignStateToLanes();
        }

        public void SetGreen(bool green)
        {
            SetPhase(green ? AutoPhase.Green : AutoPhase.Red);
        }

        public void SetState(TrafficLightState state)
        {
            if (state == TrafficLightState.Yellow)
            {
                SetListState(controlledLights, TrafficLightState.Yellow);
                return;
            }

            SetGreen(state == TrafficLightState.Green);
        }

        public bool CanLaneProceed(bool laneIsNorthSouthFlow)
        {
            return IsGreen;
        }

        public void AssignLane(Lane lane, int stopWaypointIndex = -1)
        {
            if (lane == null) { return; }
            if (!assignedLanes.Contains(lane)) { assignedLanes.Add(lane); }
            int resolvedStop = stopWaypointIndex >= 0 ? stopWaypointIndex : (lane.StopWaypointIndex >= 0 ? lane.StopWaypointIndex : defaultStopWaypointIndex);
            lane.SetTrafficLightGroup(this, true);
            lane.SetTrafficLight(null, resolvedStop);
        }

        public void AssignStateToLanes()
        {
            for (int i = assignedLanes.Count - 1; i >= 0; i--)
            {
                Lane lane = assignedLanes[i];
                if (lane == null)
                {
                    assignedLanes.RemoveAt(i);
                    continue;
                }

                AssignLane(lane);
            }
        }

        private static void SetListState(List<TrafficLightController> lights, TrafficLightState state)
        {
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                lights[i].ForceState(state);
            }
        }

        private void ApplyVisualState()
        {
            if (phase == AutoPhase.Green)
            {
                SetListState(controlledLights, TrafficLightState.Green);
            }
            else if (phase == AutoPhase.Yellow)
            {
                SetListState(controlledLights, TrafficLightState.Yellow);
            }
            else
            {
                SetListState(controlledLights, TrafficLightState.Red);
            }
        }

        private void TickAutoTimer()
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer > 0f)
            {
                return;
            }

            if (phase == AutoPhase.Green)
            {
                SetPhase(AutoPhase.Yellow);
            }
            else if (phase == AutoPhase.Yellow)
            {
                SetPhase(AutoPhase.Red);
            }
            else
            {
                SetPhase(AutoPhase.Green);
            }

            AssignStateToLanes();
        }

        private void SetPhase(AutoPhase newPhase)
        {
            phase = newPhase;

            if (phase == AutoPhase.Green)
            {
                IsGreen = true;
                phaseTimer = Mathf.Max(0.5f, greenDuration);
                if (oppositeGroup != null)
                {
                    oppositeGroup.SetPhaseFromOpposite(AutoPhase.Red);
                }
            }
            else if (phase == AutoPhase.Yellow)
            {
                IsGreen = false;
                phaseTimer = Mathf.Max(0.5f, yellowDuration);
                if (oppositeGroup != null)
                {
                    oppositeGroup.SetPhaseFromOpposite(AutoPhase.Red);
                }
            }
            else
            {
                IsGreen = false;
                phaseTimer = Mathf.Max(0.5f, redDuration);
                if (oppositeGroup != null)
                {
                    oppositeGroup.SetPhaseFromOpposite(AutoPhase.Green);
                }
            }

            ApplyVisualState();
        }

        private void SetPhaseFromOpposite(AutoPhase newPhase)
        {
            phase = newPhase;
            IsGreen = newPhase == AutoPhase.Green;
            phaseTimer = newPhase == AutoPhase.Green ? Mathf.Max(0.5f, greenDuration) :
                        newPhase == AutoPhase.Yellow ? Mathf.Max(0.5f, yellowDuration) :
                        Mathf.Max(0.5f, redDuration);
            ApplyVisualState();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugCountdown)
            {
                return;
            }

            Gizmos.color = Color.white;
            Vector3 p = transform.position + Vector3.up * 3.2f;
            Gizmos.DrawSphere(p, 0.08f);
        }
    }
}
