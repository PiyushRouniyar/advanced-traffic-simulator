using System.Collections.Generic;
using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    [DisallowMultipleComponent]
    public class TrafficLightGroup : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private string groupName = "Group";
        public KeyCode assignedKey = KeyCode.Alpha1;
        [SerializeField] private bool startGreen = true;
        [SerializeField] private List<Lane> assignedLanes = new List<Lane>();
        [SerializeField] private List<TrafficLightController> controlledLights = new List<TrafficLightController>();
        [SerializeField] private int defaultStopWaypointIndex = 0;

        public bool IsGreen { get; private set; } = true;
        public string GroupName => groupName;
        public KeyCode ActivationKey => assignedKey;

        private void Awake()
        {
            IsGreen = startGreen;
            ApplyVisualState();
            AssignStateToLanes();
        }

        private void Update()
        {
            if (assignedKey != KeyCode.None && Input.GetKeyDown(assignedKey))
            {
                ToggleGroupState();
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
            IsGreen = green;
            ApplyVisualState();
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
            SetListState(controlledLights, IsGreen ? TrafficLightState.Green : TrafficLightState.Red);
        }
    }
}
