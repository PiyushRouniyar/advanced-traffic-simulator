using UnityEngine;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class TrafficIntersectionInteractable : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string intersectionName = "Alpha-1";

        [Header("Traffic")]
        [SerializeField] private MyTrafficSystem.TrafficLights.TrafficIntersectionManager intersectionManager;
        [SerializeField] private MyTrafficSystem.TrafficLights.TrafficLightGroup[] groups;

        [Header("Emergency")]
        [SerializeField] private float emergencyAllRedDuration = 5f;

        [Header("Highlight")]
        [SerializeField] private bool enableHighlight = true;
        [SerializeField] private Renderer[] highlightRenderers;
        [SerializeField] private Color highlightColor = new Color(0.37f, 0.85f, 1f, 1f);
        [SerializeField] private float highlightIntensity = 1.4f;
        [SerializeField] private float highlightPulseSpeed = 2f;

        private Coroutine emergencyRoutine;
        private bool highlightActive;

        public string IntersectionName => intersectionName;
        public MyTrafficSystem.TrafficLights.TrafficLightGroup[] Groups => groups;
        public MyTrafficSystem.TrafficLights.TrafficIntersectionManager IntersectionManager => intersectionManager;
        public bool AutoModeEnabled { get; private set; }

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (intersectionManager == null)
            {
                intersectionManager = GetComponentInChildren<MyTrafficSystem.TrafficLights.TrafficIntersectionManager>();
            }

            if ((groups == null || groups.Length == 0) && intersectionManager != null)
            {
                groups = intersectionManager.GetComponentsInChildren<MyTrafficSystem.TrafficLights.TrafficLightGroup>(true);
            }

            if (highlightRenderers == null || highlightRenderers.Length == 0)
            {
                highlightRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Update()
        {
            UpdateHighlightVisual();
        }

        public void SetHighlightActive(bool active)
        {
            if (!enableHighlight)
            {
                return;
            }

            highlightActive = active;
        }

        public void SwitchPhase()
        {
            if (groups == null || groups.Length == 0 || intersectionManager == null)
            {
                return;
            }

            int currentGreen = GetCurrentGreenIndex();
            int next = (currentGreen + 1) % groups.Length;
            intersectionManager.SetGroupGreen(next);
        }

        public void SetAutoMode(bool enabled)
        {
            AutoModeEnabled = enabled;
            if (groups == null)
            {
                return;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null)
                {
                    groups[i].SetAutoSwitch(enabled);
                }
            }
        }

        public void TriggerEmergencyAllRed()
        {
            if (groups == null || groups.Length == 0)
            {
                return;
            }

            if (emergencyRoutine != null)
            {
                StopCoroutine(emergencyRoutine);
            }

            emergencyRoutine = StartCoroutine(EmergencyAllRedRoutine());
        }

        public int GetCurrentGreenIndex()
        {
            if (groups == null || groups.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].IsGreen)
                {
                    return i;
                }
            }

            return 0;
        }

        public string GetActiveDirectionLabel()
        {
            int idx = GetCurrentGreenIndex();
            if (groups == null || groups.Length == 0 || groups[idx] == null)
            {
                return "N/A";
            }

            return groups[idx].GroupName;
        }

        public float GetCurrentTimer()
        {
            int idx = GetCurrentGreenIndex();
            if (groups == null || groups.Length == 0 || groups[idx] == null)
            {
                return 0f;
            }

            return groups[idx].RemainingTime;
        }

        public int EstimateWaitingCars()
        {
            if (groups == null || groups.Length == 0)
            {
                return 0;
            }

            int waiting = 0;
            MyTrafficSystem.AI.TrafficCarAI[] cars = FindObjectsByType<MyTrafficSystem.AI.TrafficCarAI>(FindObjectsSortMode.None);
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i] == null)
                {
                    continue;
                }

                Vector3 carPosition = cars[i].transform.position;
                for (int g = 0; g < groups.Length; g++)
                {
                    MyTrafficSystem.TrafficLights.TrafficLightGroup group = groups[g];
                    if (group == null || group.IsGreen)
                    {
                        continue;
                    }

                    var lanes = group.AssignedLanes;
                    for (int l = 0; l < lanes.Count; l++)
                    {
                        var lane = lanes[l];
                        if (lane == null || lane.Waypoints.Count == 0)
                        {
                            continue;
                        }

                        int stopIndex = Mathf.Clamp(lane.StopWaypointIndex, 0, lane.Waypoints.Count - 1);
                        var stopWaypoint = lane.Waypoints[stopIndex];
                        if (stopWaypoint == null)
                        {
                            continue;
                        }

                        if ((stopWaypoint.transform.position - carPosition).sqrMagnitude < 36f)
                        {
                            waiting++;
                            break;
                        }
                    }
                }
            }

            return waiting;
        }

        private System.Collections.IEnumerator EmergencyAllRedRoutine()
        {
            bool previousAuto = AutoModeEnabled;
            SetAutoMode(false);

            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null)
                {
                    groups[i].ForceAllRedImmediate();
                }
            }

            yield return new WaitForSeconds(Mathf.Max(1f, emergencyAllRedDuration));

            if (intersectionManager != null)
            {
                intersectionManager.SetGroupGreen(0);
            }

            SetAutoMode(previousAuto);
            emergencyRoutine = null;
        }

        private void UpdateHighlightVisual()
        {
            if (!enableHighlight || highlightRenderers == null)
            {
                return;
            }

            float strength = highlightActive ? (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * highlightPulseSpeed))) : 0f;
            Color emission = highlightColor * (highlightIntensity * strength);

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
