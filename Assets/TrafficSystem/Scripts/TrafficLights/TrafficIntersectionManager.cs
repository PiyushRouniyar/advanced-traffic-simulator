using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    public class TrafficIntersectionManager : MonoBehaviour
    {
        [System.Serializable]
        public class IntersectionPhase
        {
            public string phaseName = "Phase";
            public float duration = 10f;
            public List<TrafficLightGroup> greenGroups = new List<TrafficLightGroup>();
            public List<TrafficLightGroup> redGroups = new List<TrafficLightGroup>();
            public bool pedestrianPhase;
        }

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private float yellowTransitionDuration = 2f;
        [SerializeField] private List<IntersectionPhase> phases = new List<IntersectionPhase>();

        private int currentPhaseIndex;
        private float timer;
        private bool inYellowTransition;

        private void Start()
        {
            if (!runOnStart || phases.Count == 0) { return; }
            ApplyPhase(0);
        }

        private void Update()
        {
            if (phases.Count == 0) { return; }
            timer -= Time.deltaTime;
            if (timer > 0f) { return; }

            if (!inYellowTransition)
            {
                SetCurrentGreenToYellow();
                inYellowTransition = true;
                timer = Mathf.Max(0.5f, yellowTransitionDuration);
                return;
            }

            int next = (currentPhaseIndex + 1) % phases.Count;
            ApplyPhase(next);
        }

        private void SetCurrentGreenToYellow()
        {
            IntersectionPhase phase = phases[currentPhaseIndex];
            for (int i = 0; i < phase.greenGroups.Count; i++)
            {
                TrafficLightGroup group = phase.greenGroups[i];
                if (group != null) { group.SetState(TrafficLightState.Yellow); }
            }
        }

        private void ApplyPhase(int index)
        {
            currentPhaseIndex = index;
            IntersectionPhase phase = phases[currentPhaseIndex];
            inYellowTransition = false;
            timer = Mathf.Max(1f, phase.duration);

            for (int i = 0; i < phase.greenGroups.Count; i++)
            {
                TrafficLightGroup group = phase.greenGroups[i];
                if (group != null)
                {
                    group.SetState(TrafficLightState.Green);
                    group.AssignStateToLanes();
                }
            }

            for (int i = 0; i < phase.redGroups.Count; i++)
            {
                TrafficLightGroup group = phase.redGroups[i];
                if (group != null)
                {
                    group.SetState(TrafficLightState.Red);
                    group.AssignStateToLanes();
                }
            }
        }
    }
}
