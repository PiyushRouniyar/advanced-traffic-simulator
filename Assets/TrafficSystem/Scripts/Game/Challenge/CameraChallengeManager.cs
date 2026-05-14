using System;
using System.Collections.Generic;
using System.Text;
using MyTrafficSystem.AI;
using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.Lanes;
using MyTrafficSystem.Managers;
using MyTrafficSystem.Pedestrians;
using MyTrafficSystem.TrafficLights;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay.Challenge
{
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public class CameraChallengeManager : MonoBehaviour
    {
        [Serializable]
        private class LaneLiveStat
        {
            public Lane lane;
            public int activeVehicles;
            public int waitingVehicles;
            public float congestion;
        }

        public static CameraChallengeManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private CCTVCameraSystem cctv;
        [SerializeField] private MasterTrafficLightController masterLights;

        [Header("Challenge")]
        [SerializeField] private float challengeDurationSeconds = 30f;
        [SerializeField] private float sampleInterval = 0.2f;
        [SerializeField] private float fallbackMonitorRadius = 120f;
        [SerializeField] private LayerMask vehicleDetectionMask = ~0;
        [SerializeField] private bool requireCameraForwardVisibility = true;
        [SerializeField] private bool showVehicleDetectionOverlay = true;

        [Header("UI")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Button monitorButton;
        [SerializeField] private TextMeshProUGUI cameraText;
        [SerializeField] private TextMeshProUGUI intersectionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI flowText;
        [SerializeField] private TextMeshProUGUI incidentText;
        [SerializeField] private TextMeshProUGUI laneListText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private CanvasGroup resultGroup;

        private readonly List<LaneLiveStat> laneStats = new List<LaneLiveStat>();
        private readonly HashSet<Lane> monitoredLaneSet = new HashSet<Lane>();
        private readonly HashSet<TrafficIntersectionManager> monitoredIntersectionSet = new HashSet<TrafficIntersectionManager>();
        private readonly HashSet<TrafficCarAI> trackedVehicles = new HashSet<TrafficCarAI>();
        private readonly HashSet<TrafficCarAI> currentFrameVehicles = new HashSet<TrafficCarAI>();
        private readonly List<TrafficCarAI> enteredVehicles = new List<TrafficCarAI>();
        private readonly List<TrafficCarAI> exitedVehicles = new List<TrafficCarAI>();

        private readonly Collider[] overlapBuffer = new Collider[512];

        private float timer;
        private float sampleTimer;
        private bool active;
        private int incidents;
        private int score;
        private float cumulativeCongestion;
        private int samples;
        private int vehiclesDetected;
        private int waitingVehiclesDetected;
        private float latestAverageCongestion;
        private CCTVCameraPoint activeCameraPoint;
        private float activeMonitorRadius;
        private float activeVerticalTolerance;
        private float activeIntersectionInfluenceRadius;
        private float baseCarSpawnInterval = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<CameraChallengeManager>(FindObjectsInactive.Include) != null) return;
            GameObject go = new GameObject("CameraChallengeManager");
            go.AddComponent<CameraChallengeManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (cctv == null) cctv = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            if (masterLights == null) masterLights = FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include);

            BuildUIIfMissing();
            WireUI();
            RefreshIdleUI();
        }

        private void Update()
        {
            if (cctv == null) return;

            if (!active)
            {
                RefreshIdleUI();
                return;
            }

            timer -= Time.deltaTime;
            sampleTimer -= Time.deltaTime;

            if (sampleTimer <= 0f)
            {
                sampleTimer = Mathf.Max(0.05f, sampleInterval);
                EvaluateLiveMetrics();
                ApplyPressureTick();
                UpdateActiveUI();
            }

            if (timer <= 0f)
            {
                EndChallenge();
            }
        }

        private void OnGUI()
        {
            if (!active || !showVehicleDetectionOverlay || trackedVehicles.Count == 0 || cctv == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            GUI.color = new Color(0.2f, 1f, 0.8f, 0.95f);
            foreach (TrafficCarAI car in trackedVehicles)
            {
                if (car == null) continue;

                Vector3 world = car.transform.position + Vector3.up * 1.2f;
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue;

                float y = Screen.height - screen.y;
                Rect box = new Rect(screen.x - 24f, y - 24f, 48f, 48f);
                GUI.Box(box, string.Empty);

                string laneName = car.CurrentLane != null ? car.CurrentLane.LaneName : "N/A";
                GUI.Label(new Rect(screen.x - 45f, y - 40f, 150f, 20f), laneName);
            }
            GUI.color = Color.white;
        }

        public void StartCurrentCameraChallenge()
        {
            if (active || cctv == null || cctv.ActivePoint == null) return;

            active = true;
            incidents = 0;
            score = 0;
            cumulativeCongestion = 0f;
            samples = 0;
            timer = challengeDurationSeconds;
            sampleTimer = 0f;
            vehiclesDetected = 0;
            waitingVehiclesDetected = 0;
            latestAverageCongestion = 0f;

            activeCameraPoint = cctv.ActivePoint;
            ConfigureMonitoringContextForActiveCamera();
            trackedVehicles.Clear();
            currentFrameVehicles.Clear();
            enteredVehicles.Clear();
            exitedVehicles.Clear();

            cctv.SetCameraSelectionLocked(true);
            SetMonitorButtonState(false, "MONITORING...");
            HideResult();
        }

        public void ReportIncident(Vector3 worldPos, Lane incidentLane = null)
        {
            if (!active) return;
            if (incidentLane != null && !monitoredLaneSet.Contains(incidentLane)) return;
            if (!IsPointInsideMonitoringArea(worldPos, null)) return;

            incidents++;
            score -= 120;
            if (incidentText != null)
            {
                incidentText.text = $"INCIDENTS: {incidents}  ? LOCAL INCIDENT";
                incidentText.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
        }

        private void EndChallenge()
        {
            active = false;
            cctv.SetCameraSelectionLocked(false);
            SetMonitorButtonState(true, "MONITOR");

            float avgCongestion = samples > 0 ? cumulativeCongestion / samples : 0f;
            float flow = 1f - avgCongestion;
            string rating = flow >= 0.8f && incidents == 0 ? "EXCELLENT" : flow >= 0.6f ? "GOOD" : flow >= 0.4f ? "FAIR" : "POOR";
            int finalScore = Mathf.Max(0, score);

            ShowResult($"SHIFT COMPLETE\nTRAFFIC FLOW: {rating}\nINCIDENTS: {incidents}\nFINAL SCORE: {finalScore}");
        }

        private void ConfigureMonitoringContextForActiveCamera()
        {
            laneStats.Clear();
            monitoredLaneSet.Clear();
            monitoredIntersectionSet.Clear();

            if (activeCameraPoint == null) return;

            activeMonitorRadius = activeCameraPoint.MonitorRadius;
            activeVerticalTolerance = activeCameraPoint.MonitorVerticalTolerance;
            activeIntersectionInfluenceRadius = activeCameraPoint.IntersectionInfluenceRadius;

            IReadOnlyList<Lane> assignedLanes = activeCameraPoint.AssignedLanes;
            for (int i = 0; i < assignedLanes.Count; i++)
            {
                Lane lane = assignedLanes[i];
                if (lane != null) monitoredLaneSet.Add(lane);
            }

            IReadOnlyList<TrafficIntersectionManager> assignedIntersections = activeCameraPoint.AssignedIntersections;
            for (int i = 0; i < assignedIntersections.Count; i++)
            {
                TrafficIntersectionManager intersection = assignedIntersections[i];
                if (intersection != null) monitoredIntersectionSet.Add(intersection);
            }

            if (activeCameraPoint.MonitoredIntersection != null)
            {
                monitoredIntersectionSet.Add(activeCameraPoint.MonitoredIntersection);
            }

            if (monitoredLaneSet.Count == 0)
            {
                BuildFallbackLanesFromZone(activeCameraPoint.transform.position, Mathf.Max(10f, activeMonitorRadius > 0f ? activeMonitorRadius : fallbackMonitorRadius));
            }

            foreach (Lane lane in monitoredLaneSet)
            {
                if (lane != null) laneStats.Add(new LaneLiveStat { lane = lane });
            }
        }

        private void BuildFallbackLanesFromZone(Vector3 center, float radius)
        {
            Lane[] lanes = FindObjectsByType<Lane>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lanes.Length; i++)
            {
                Lane lane = lanes[i];
                if (lane == null) continue;

                Vector3 a = lane.StartWaypoint != null ? lane.StartWaypoint.transform.position : lane.transform.position;
                Vector3 b = lane.EndWaypoint != null ? lane.EndWaypoint.transform.position : lane.transform.position;
                float dist = Mathf.Min(Vector3.Distance(center, a), Vector3.Distance(center, b));
                if (dist <= radius)
                {
                    monitoredLaneSet.Add(lane);
                }
            }
        }

        private void EvaluateLiveMetrics()
        {
            currentFrameVehicles.Clear();

            Vector3 origin = activeCameraPoint != null ? activeCameraPoint.transform.position : transform.position;
            float radius = Mathf.Max(10f, activeMonitorRadius > 0f ? activeMonitorRadius : fallbackMonitorRadius);
            int overlapCount = Physics.OverlapSphereNonAlloc(origin, radius, overlapBuffer, vehicleDetectionMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null) continue;

                TrafficCarAI car = col.GetComponentInParent<TrafficCarAI>();
                if (car == null || !car.isActiveAndEnabled) continue;
                if (!IsCarInsideActiveMonitorContext(car)) continue;

                currentFrameVehicles.Add(car);
            }

            enteredVehicles.Clear();
            exitedVehicles.Clear();
            foreach (TrafficCarAI car in currentFrameVehicles)
            {
                if (!trackedVehicles.Contains(car)) enteredVehicles.Add(car);
            }

            foreach (TrafficCarAI car in trackedVehicles)
            {
                if (car == null || !currentFrameVehicles.Contains(car)) exitedVehicles.Add(car);
            }

            for (int i = 0; i < enteredVehicles.Count; i++) trackedVehicles.Add(enteredVehicles[i]);
            for (int i = 0; i < exitedVehicles.Count; i++) trackedVehicles.Remove(exitedVehicles[i]);

            for (int i = 0; i < laneStats.Count; i++)
            {
                laneStats[i].activeVehicles = 0;
                laneStats[i].waitingVehicles = 0;
                laneStats[i].congestion = 0f;
            }

            int localWaiting = 0;
            foreach (TrafficCarAI car in trackedVehicles)
            {
                if (car == null) continue;
                Lane lane = car.CurrentLane;
                if (lane == null || !monitoredLaneSet.Contains(lane)) continue;

                for (int i = 0; i < laneStats.Count; i++)
                {
                    LaneLiveStat st = laneStats[i];
                    if (st.lane != lane) continue;

                    st.activeVehicles++;
                    Rigidbody rb = car.GetComponent<Rigidbody>();
                    if (rb != null && rb.linearVelocity.magnitude < 0.45f)
                    {
                        st.waitingVehicles++;
                        localWaiting++;
                    }
                    break;
                }
            }

            float totalCongestion = 0f;
            for (int i = 0; i < laneStats.Count; i++)
            {
                LaneLiveStat st = laneStats[i];
                st.congestion = st.activeVehicles <= 0
                    ? 0f
                    : Mathf.Clamp01((st.waitingVehicles + Mathf.Max(0, st.activeVehicles - 5) * 0.35f) / Mathf.Max(1f, st.activeVehicles));
                totalCongestion += st.congestion;
            }

            latestAverageCongestion = laneStats.Count > 0 ? totalCongestion / laneStats.Count : 0f;
            cumulativeCongestion += latestAverageCongestion;
            samples++;

            vehiclesDetected = trackedVehicles.Count;
            waitingVehiclesDetected = localWaiting;

            int efficiencyGain = Mathf.RoundToInt((1f - latestAverageCongestion) * 18f);
            int incidentPenalty = incidents * 2;
            score += Mathf.Max(0, efficiencyGain - incidentPenalty);
        }

        private bool IsCarInsideActiveMonitorContext(TrafficCarAI car)
        {
            if (car == null || car.CurrentLane == null) return false;
            if (!monitoredLaneSet.Contains(car.CurrentLane)) return false;

            Vector3 p = car.transform.position;
            if (!IsPointInsideMonitoringArea(p, car.CurrentLane)) return false;

            return true;
        }

        private bool IsPointInsideMonitoringArea(Vector3 worldPos, Lane lane)
        {
            if (activeCameraPoint == null) return false;

            Vector3 origin = activeCameraPoint.transform.position;
            Vector3 toPoint = worldPos - origin;
            if (Mathf.Abs(toPoint.y) > activeVerticalTolerance) return false;

            float dist = toPoint.magnitude;
            float radius = Mathf.Max(10f, activeMonitorRadius > 0f ? activeMonitorRadius : fallbackMonitorRadius);
            if (dist > radius) return false;

            if (requireCameraForwardVisibility)
            {
                float angle = Vector3.Angle(activeCameraPoint.transform.forward, toPoint.normalized);
                if (angle > activeCameraPoint.FieldOfView * 0.65f) return false;
            }

            if (monitoredIntersectionSet.Count > 0)
            {
                bool nearAssignedIntersection = false;
                foreach (TrafficIntersectionManager intersection in monitoredIntersectionSet)
                {
                    if (intersection == null) continue;
                    if (Vector3.Distance(worldPos, intersection.transform.position) <= activeIntersectionInfluenceRadius)
                    {
                        nearAssignedIntersection = true;
                        break;
                    }
                }

                if (!nearAssignedIntersection && lane == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyPressureTick()
        {
            float t = 1f - Mathf.Clamp01(timer / Mathf.Max(1f, challengeDurationSeconds));
            AutomaticTrafficSpawner[] spawners = FindObjectsByType<AutomaticTrafficSpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; i++)
            {
                AutomaticTrafficSpawner sp = spawners[i];
                if (sp == null) continue;

                if (baseCarSpawnInterval < 0f) baseCarSpawnInterval = sp.SpawnInterval;
                sp.SpawnInterval = Mathf.Lerp(baseCarSpawnInterval, Mathf.Max(0.3f, baseCarSpawnInterval * 0.45f), t);
                sp.MaxActiveCars = Mathf.RoundToInt(Mathf.Lerp(60f, 180f, t));
                sp.StartTraffic();
            }

            CitizenSpawner[] citizenSpawners = FindObjectsByType<CitizenSpawner>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < citizenSpawners.Length; i++)
            {
                citizenSpawners[i]?.StartSpawning();
            }
        }

        private void UpdateActiveUI()
        {
            float flow = 1f - latestAverageCongestion;

            if (cameraText != null) cameraText.text = $"CAMERA: {cctv.ActiveCameraLabel}";
            if (intersectionText != null) intersectionText.text = $"INTERSECTION: {cctv.ActiveIntersectionName}";
            if (timerText != null) timerText.text = $"TIMER: {Mathf.CeilToInt(timer):00}s";
            if (scoreText != null) scoreText.text = $"SCORE: {Mathf.Max(0, score)}";
            if (congestionText != null) congestionText.text = $"CONGESTION: {(latestAverageCongestion * 100f):0}% ({GetCongestionStatus(latestAverageCongestion)})";
            if (flowText != null) flowText.text = $"FLOW: {(flow * 100f):0}%";
            if (incidentText != null) incidentText.text = $"INCIDENTS: {incidents}";

            if (laneListText != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"VEHICLES DETECTED: {vehiclesDetected}");
                sb.AppendLine($"WAITING VEHICLES: {waitingVehiclesDetected}");
                sb.AppendLine($"ZONE: {Mathf.RoundToInt(activeMonitorRadius)}m");
                if (enteredVehicles.Count > 0) sb.AppendLine($"ENTERED: +{enteredVehicles.Count}");
                if (exitedVehicles.Count > 0) sb.AppendLine($"LEFT: -{exitedVehicles.Count}");
                sb.AppendLine();

                int max = Mathf.Min(10, laneStats.Count);
                for (int i = 0; i < max; i++)
                {
                    LaneLiveStat st = laneStats[i];
                    string status = GetCongestionStatus(st.congestion);
                    sb.AppendLine($"{st.lane.LaneName}: {st.activeVehicles} cars | wait {st.waitingVehicles} | {status}");
                }
                laneListText.text = sb.ToString();
            }
        }

        private static string GetCongestionStatus(float congestion01)
        {
            if (congestion01 >= 0.72f) return "HIGH";
            if (congestion01 >= 0.45f) return "MED";
            if (congestion01 >= 0.22f) return "LOW";
            return "CLEAR";
        }

        private void RefreshIdleUI()
        {
            if (cameraText != null && cctv != null) cameraText.text = $"CAMERA: {cctv.ActiveCameraLabel}";
            if (intersectionText != null && cctv != null) intersectionText.text = $"INTERSECTION: {cctv.ActiveIntersectionName}";
            if (timerText != null) timerText.text = "TIMER: --";
            if (scoreText != null) scoreText.text = "SCORE: --";
            if (congestionText != null) congestionText.text = "CONGESTION: --";
            if (flowText != null) flowText.text = "FLOW: --";
            if (incidentText != null)
            {
                incidentText.text = "INCIDENTS: 0";
                incidentText.color = new Color(0.84f, 0.92f, 1f, 1f);
            }
            if (laneListText != null) laneListText.text = "Press MONITOR to start a localized CCTV shift.";
        }

        private void BuildUIIfMissing()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }

            monitorButton = null;
            cameraText = null;
            intersectionText = null;
            timerText = null;
            scoreText = null;
            congestionText = null;
            flowText = null;
            incidentText = null;
            laneListText = null;
            resultText = null;
            resultGroup = null;

            canvas = new GameObject("CameraChallengeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Transform root = canvas.transform;
            Image topLeft = Panel(root, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, -105f), new Vector2(500f, 190f));
            Image topRight = Panel(root, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-220f, -100f), new Vector2(450f, 170f));
            Image left = Panel(root, "LeftPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, -20f), new Vector2(540f, 500f));
            Image centerBottom = Panel(root, "CenterBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(340f, 90f));
            resultGroup = Overlay(root, "ResultOverlay");
            resultText = Label(resultGroup.transform, "ResultText", "", new Vector2(0f, 0f), 44f, true);

            cameraText = Label(topLeft.transform, "CameraText", "CAMERA: --", new Vector2(0f, 60f), 26f, false);
            intersectionText = Label(topLeft.transform, "IntersectionText", "INTERSECTION: --", new Vector2(0f, 20f), 24f, false);
            timerText = Label(topLeft.transform, "TimerText", "TIMER: --", new Vector2(0f, -20f), 24f, false);
            scoreText = Label(topLeft.transform, "ScoreText", "SCORE: --", new Vector2(0f, -60f), 24f, false);

            congestionText = Label(topRight.transform, "CongText", "CONGESTION: --", new Vector2(0f, 40f), 24f, false);
            flowText = Label(topRight.transform, "FlowText", "FLOW: --", new Vector2(0f, 0f), 24f, false);
            incidentText = Label(topRight.transform, "IncidentText", "INCIDENTS: 0", new Vector2(0f, -40f), 24f, false);

            laneListText = Label(left.transform, "LaneListText", "", new Vector2(0f, 0f), 22f, false);
            laneListText.alignment = TextAlignmentOptions.TopLeft;
            laneListText.rectTransform.sizeDelta = new Vector2(500f, 450f);

            monitorButton = CreateButton(centerBottom.transform, "MONITOR", new Vector2(0f, 0f), new Vector2(250f, 54f));
            HideResult();
        }

        private void WireUI()
        {
            if (monitorButton != null)
            {
                monitorButton.onClick.RemoveAllListeners();
                monitorButton.onClick.AddListener(StartCurrentCameraChallenge);
            }
        }

        private void SetMonitorButtonState(bool interactable, string label)
        {
            if (monitorButton == null) return;
            monitorButton.interactable = interactable;
            TextMeshProUGUI txt = monitorButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = label;
        }

        private void ShowResult(string text)
        {
            if (resultGroup == null || resultText == null) return;
            resultText.text = text;
            resultGroup.alpha = 1f;
            resultGroup.interactable = true;
            resultGroup.blocksRaycasts = true;
        }

        private void HideResult()
        {
            if (resultGroup == null) return;
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;
            resultGroup.blocksRaycasts = false;
        }

        private static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.04f, 0.07f, 0.1f, 0.9f);
            return img;
        }

        private static CanvasGroup Overlay(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            return go.GetComponent<CanvasGroup>();
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 pos, float fontSize, bool centered)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(450f, 90f);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.85f, 0.94f, 1f, 1f);
            tmp.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.72f, 0.95f, 0.95f);

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.2f, 0.8f, 1f, 1f);
            cb.pressedColor = new Color(0.08f, 0.6f, 0.82f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(go.transform, false);
            RectTransform tr = txtObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 28f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.95f, 0.98f, 1f, 1f);

            return btn;
        }
    }
}
