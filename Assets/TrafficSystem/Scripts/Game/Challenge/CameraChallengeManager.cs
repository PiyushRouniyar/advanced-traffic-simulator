using System;
using System.Collections.Generic;
using System.Text;
using MyTrafficSystem.AI;
using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.Gameplay.FreeMode;
using MyTrafficSystem.Lanes;
using MyTrafficSystem.Managers;
using MyTrafficSystem.Pedestrians;
using MyTrafficSystem.TrafficLights;
using MyTrafficSystem.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        [SerializeField] private FreeModeManager freeModeManager;

        [Header("Challenge")]
        [SerializeField] private float challengeDurationSeconds = 30f;
        [SerializeField] private float sampleInterval = 0.2f;
        [SerializeField] private float fallbackMonitorRadius = 120f;
        [SerializeField] private LayerMask vehicleDetectionMask = ~0;
        [SerializeField] private bool requireCameraForwardVisibility = false;
        [SerializeField] private bool strictIncidentPovOnly = true;
        [SerializeField] private bool strictAssignedLaneOnly = false;
        [SerializeField] private bool showVehicleDetectionOverlay = true;
        [SerializeField] private bool showDetectionStateOnCars = true;
        [SerializeField] private bool useGlobalVehicleScanFallback = false;
        [SerializeField] private Color movingVehicleColor = new Color(0.2f, 1f, 0.35f, 0.95f);
        [SerializeField] private Color alertVehicleColor = new Color(1f, 0.25f, 0.25f, 0.98f);
        [SerializeField] private float pathDeviationThreshold = 8f;
        [SerializeField] private float networkFeedRefreshInterval = 0.55f;
        [SerializeField] private int mediumIncidentThreshold = 50;
        [SerializeField] private int highIncidentThreshold = 100;

        [Header("UI")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Button monitorButton;
        [SerializeField] private Button freeRoamButton;
        [SerializeField] private TextMeshProUGUI cameraText;
        [SerializeField] private TextMeshProUGUI intersectionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI flowText;
        [SerializeField] private TextMeshProUGUI incidentText;
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI laneListText;
        [SerializeField] private TextMeshProUGUI cameraBadgeText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private CanvasGroup resultGroup;
        [SerializeField] private Button restartButton;

        private readonly List<LaneLiveStat> laneStats = new List<LaneLiveStat>();
        private readonly HashSet<Lane> monitoredLaneSet = new HashSet<Lane>();
        private readonly HashSet<TrafficIntersectionManager> monitoredIntersectionSet = new HashSet<TrafficIntersectionManager>();
        private readonly HashSet<TrafficCarAI> trackedVehicles = new HashSet<TrafficCarAI>();
        private readonly HashSet<TrafficCarAI> currentFrameVehicles = new HashSet<TrafficCarAI>();
        private readonly HashSet<Rigidbody> trackedRigidVehicles = new HashSet<Rigidbody>();
        private readonly HashSet<Rigidbody> currentFrameRigidVehicles = new HashSet<Rigidbody>();
        private readonly HashSet<TrafficDetectableVehicle> trackedDetectableVehicles = new HashSet<TrafficDetectableVehicle>();
        private readonly HashSet<TrafficDetectableVehicle> currentFrameDetectableVehicles = new HashSet<TrafficDetectableVehicle>();
        private readonly List<TrafficCarAI> enteredVehicles = new List<TrafficCarAI>();
        private readonly List<TrafficCarAI> exitedVehicles = new List<TrafficCarAI>();
        private readonly HashSet<TrafficCarAI> waitingVehicles = new HashSet<TrafficCarAI>();
        private readonly Dictionary<TrafficCarAI, float> collisionIncidentUntil = new Dictionary<TrafficCarAI, float>();
        private readonly Dictionary<TrafficCarAI, GameObject> markerByVehicle = new Dictionary<TrafficCarAI, GameObject>();
        private readonly List<string> cameraNetworkLines = new List<string>();
        private readonly Collider[] networkOverlapBuffer = new Collider[256];

        private readonly Collider[] overlapBuffer = new Collider[512];

        private float timer;
        private float sampleTimer;
        private bool active;
        private int localCollisionIncidents;
        private int lastKnownGlobalCollisions;
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
        private float networkFeedTimer;
        private bool isReady;
        private bool hasLoggedReady;
        [SerializeField] private float incidentHighlightDuration = 3f;

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

            EnsureReady(log: false);
        }

        private void OnEnable()
        {
            TrafficIncidentSystem.IncidentReported += OnTrafficIncidentReported;
        }

        private void OnDisable()
        {
            TrafficIncidentSystem.IncidentReported -= OnTrafficIncidentReported;
        }

        private void Update()
        {
            if (!isReady)
            {
                EnsureReady(log: false);
                if (!isReady) return;
            }

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
                RefreshCameraNetworkFeed(false);
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
                bool alert = IsCollisionHighlighted(car) || IsCarOffExpectedPath(car);
                GUI.color = alert ? alertVehicleColor : movingVehicleColor;
                Rect box = new Rect(screen.x - 24f, y - 24f, 48f, 48f);
                GUI.Box(box, string.Empty);

                string laneName = car.CurrentLane != null ? car.CurrentLane.LaneName : "N/A";
                GUI.Label(new Rect(screen.x - 45f, y - 40f, 150f, 20f), laneName);
            }
            GUI.color = Color.white;
        }

        public void StartCurrentCameraChallenge()
        {
            if (!EnsureReady()) return;
            if (active || cctv == null || cctv.ActivePoint == null) return;

            active = true;
            localCollisionIncidents = 0;
            score = 0;
            cumulativeCongestion = 0f;
            samples = 0;
            timer = challengeDurationSeconds;
            sampleTimer = 0f;
            vehiclesDetected = 0;
            waitingVehiclesDetected = 0;
            latestAverageCongestion = 0f;
            networkFeedTimer = 0f;

            activeCameraPoint = cctv.ActivePoint;
            ConfigureMonitoringContextForActiveCamera();
            trackedVehicles.Clear();
            currentFrameVehicles.Clear();
            enteredVehicles.Clear();
            exitedVehicles.Clear();
            waitingVehicles.Clear();
            RefreshCameraNetworkFeed(true);

            if (freeModeManager != null) freeModeManager.ExitFreeMode();
            cctv.SetFreeRoamEnabled(false);
            cctv.SetCameraSelectionLocked(true);
            SetMonitorButtonState(false, "MONITORING...");
            HideResult();
        }

        public void EnterFreeRoamMode()
        {
            EnsureReady(log: false);
            active = false;
            cctv?.SetCameraSelectionLocked(false);
            if (freeModeManager != null)
            {
                freeModeManager.EnterFreeMode();
            }
            else
            {
                cctv?.SetFreeRoamEnabled(true);
            }
            SetMonitorButtonState(true, "MONITOR");
            HideResult();
            ClearVehicleMarkers();
            trackedVehicles.Clear();
            currentFrameVehicles.Clear();
            enteredVehicles.Clear();
            exitedVehicles.Clear();
            waitingVehicles.Clear();
            if (laneListText != null)
            {
                laneListText.text = "FREE ROAM ENABLED\nSwitch cameras and roam the full city.\nPress MONITOR to start localized challenge.";
            }
        }

        private void EndChallenge()
        {
            active = false;
            cctv.SetCameraSelectionLocked(false);
            SetMonitorButtonState(true, "MONITOR");
            ClearVehicleMarkers();

            float avgCongestion = samples > 0 ? cumulativeCongestion / samples : 0f;
            float flow = 1f - avgCongestion;
            int finalScore = Mathf.Max(0, score);
            string incidentGrade = GetIncidentGrade(localCollisionIncidents);
            float performanceScore01 = GetPerformanceScore01(flow, localCollisionIncidents);
            string overallGrade = GetOverallGrade(performanceScore01);

            ShowResult(
                $"SHIFT COMPLETE\n" +
                $"FLOW: {(flow * 100f):0}%  ({GetFlowGrade(flow)})\n" +
                $"GLOBAL COLLISIONS: {lastKnownGlobalCollisions}\n" +
                $"LOCAL CAMERA COLLISIONS: {localCollisionIncidents}  ({incidentGrade})\n" +
                $"OVERALL GRADE: {overallGrade}\n" +
                $"FINAL SCORE: {finalScore}");
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

            // Expand one hop from assigned lanes so lane-switching cars in the same intersection remain detectable.
            List<Lane> seedLanes = new List<Lane>(monitoredLaneSet);
            for (int i = 0; i < seedLanes.Count; i++)
            {
                Lane lane = seedLanes[i];
                if (lane == null) continue;
                IReadOnlyList<Lane> connected = lane.ConnectedLanes;
                for (int j = 0; j < connected.Count; j++)
                {
                    Lane next = connected[j];
                    if (next != null) monitoredLaneSet.Add(next);
                }
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
            currentFrameRigidVehicles.Clear();
            currentFrameDetectableVehicles.Clear();

            Vector3 origin = activeCameraPoint != null ? activeCameraPoint.transform.position : transform.position;
            float radius = Mathf.Max(10f, activeMonitorRadius > 0f ? activeMonitorRadius : fallbackMonitorRadius);
            if (activeCameraPoint != null)
            {
                radius = Mathf.Max(radius, activeCameraPoint.MaxViewRange);
            }
            if (monitoredLaneSet.Count == 0 && monitoredIntersectionSet.Count == 0)
            {
                radius *= 1.5f;
            }
            int overlapCount = Physics.OverlapSphereNonAlloc(origin, radius, overlapBuffer, vehicleDetectionMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = overlapBuffer[i];
                if (col == null) continue;

                TrafficCarAI car = col.GetComponentInParent<TrafficCarAI>();
                if (car != null && car.isActiveAndEnabled)
                {
                    if (!IsCarInsideActiveMonitorContext(car)) continue;
                    currentFrameVehicles.Add(car);
                    continue;
                }

                Rigidbody rb = col.attachedRigidbody != null ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
                if (rb == null || !rb.gameObject.activeInHierarchy) continue;
                if (rb.GetComponentInParent<TrafficCarAI>() != null) continue;

                TrafficDetectableVehicle marked = rb.GetComponentInParent<TrafficDetectableVehicle>();
                if (marked != null)
                {
                    if (IsPointInsideMonitoringArea(marked.DetectionPosition, null))
                    {
                        currentFrameDetectableVehicles.Add(marked);
                    }
                    continue;
                }

                if (!IsLikelyVehicleObject(rb.gameObject)) continue;
                if (!IsPointInsideMonitoringArea(rb.worldCenterOfMass, null)) continue;
                currentFrameRigidVehicles.Add(rb);
            }

            if (useGlobalVehicleScanFallback && currentFrameVehicles.Count == 0 && currentFrameRigidVehicles.Count == 0 && currentFrameDetectableVehicles.Count == 0)
            {
                TrafficDetectableVehicle[] markedVehicles = FindObjectsByType<TrafficDetectableVehicle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < markedVehicles.Length; i++)
                {
                    TrafficDetectableVehicle marked = markedVehicles[i];
                    if (marked == null || !marked.isActiveAndEnabled) continue;
                    if (!IsPointInsideMonitoringArea(marked.DetectionPosition, null)) continue;
                    currentFrameDetectableVehicles.Add(marked);
                }

                TrafficCarAI[] allCars = FindObjectsByType<TrafficCarAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < allCars.Length; i++)
                {
                    TrafficCarAI car = allCars[i];
                    if (car == null || !car.isActiveAndEnabled) continue;
                    if (!IsCarInsideActiveMonitorContext(car)) continue;
                    currentFrameVehicles.Add(car);
                }

                Rigidbody[] allBodies = FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < allBodies.Length; i++)
                {
                    Rigidbody rb = allBodies[i];
                    if (rb == null || !rb.gameObject.activeInHierarchy) continue;
                    if (rb.GetComponentInParent<TrafficCarAI>() != null) continue;
                    if (!IsLikelyVehicleObject(rb.gameObject)) continue;
                    if (!IsPointInsideMonitoringArea(rb.worldCenterOfMass, null)) continue;
                    currentFrameRigidVehicles.Add(rb);
                }
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
            waitingVehicles.Clear();
            if (laneStats.Count == 0 && trackedVehicles.Count > 0)
            {
                HashSet<Lane> dynamicLanes = new HashSet<Lane>();
                foreach (TrafficCarAI car in trackedVehicles)
                {
                    if (car?.CurrentLane == null) continue;
                    if (dynamicLanes.Add(car.CurrentLane))
                    {
                        laneStats.Add(new LaneLiveStat { lane = car.CurrentLane });
                    }
                }
            }

            foreach (TrafficCarAI car in trackedVehicles)
            {
                if (car == null) continue;
                Lane lane = car.CurrentLane;
                if (lane == null) continue;
                if (monitoredLaneSet.Count > 0 && !monitoredLaneSet.Contains(lane)) continue;

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
                        waitingVehicles.Add(car);
                    }
                    break;
                }
            }

            trackedRigidVehicles.Clear();
            trackedDetectableVehicles.Clear();
            foreach (Rigidbody rb in currentFrameRigidVehicles)
            {
                if (rb == null) continue;
                trackedRigidVehicles.Add(rb);
                if (rb.linearVelocity.magnitude < 0.45f)
                {
                    localWaiting++;
                }
            }

            foreach (TrafficDetectableVehicle marked in currentFrameDetectableVehicles)
            {
                if (marked == null) continue;
                trackedDetectableVehicles.Add(marked);
                if (marked.SpeedMps < 0.45f)
                {
                    localWaiting++;
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

            vehiclesDetected = trackedVehicles.Count + trackedRigidVehicles.Count + trackedDetectableVehicles.Count;
            waitingVehiclesDetected = localWaiting;

            int efficiencyGain = Mathf.RoundToInt((1f - latestAverageCongestion) * 18f);
            int incidentPenalty = localCollisionIncidents * 2;
            score += Mathf.Max(0, efficiencyGain - incidentPenalty);
            UpdateVehicleMarkers();
        }

        private bool IsCarInsideActiveMonitorContext(TrafficCarAI car)
        {
            if (car == null) return false;

            Lane lane = car.CurrentLane;
            if (strictAssignedLaneOnly && monitoredLaneSet.Count > 0)
            {
                if (lane == null || !monitoredLaneSet.Contains(lane)) return false;
            }

            Vector3 p = car.transform.position;
            if (!IsPointInsideMonitoringArea(p, lane)) return false;

            return true;
        }

        private bool IsPointInsideMonitoringArea(Vector3 worldPos, Lane lane)
        {
            if (activeCameraPoint == null) return false;

            Camera activeViewCamera = Camera.main;
            if (activeViewCamera != null)
            {
                Vector3 vp = activeViewCamera.WorldToViewportPoint(worldPos);
                bool onScreen = vp.z > 1f && vp.x >= -0.05f && vp.x <= 1.05f && vp.y >= -0.05f && vp.y <= 1.05f;
                if (onScreen)
                {
                    return true;
                }
            }

            Vector3 origin = activeCameraPoint.transform.position;
            Vector3 toPoint = worldPos - origin;
            bool hasExplicitCameraContext = monitoredLaneSet.Count > 0 || monitoredIntersectionSet.Count > 0;
            float effectiveVerticalTolerance = hasExplicitCameraContext
                ? Mathf.Max(4f, activeVerticalTolerance)
                : Mathf.Max(45f, activeVerticalTolerance, activeMonitorRadius * 0.65f);
            if (Mathf.Abs(toPoint.y) > effectiveVerticalTolerance) return false;

            float dist = toPoint.magnitude;
            float horizontalDist = new Vector2(toPoint.x, toPoint.z).magnitude;
            float radius = Mathf.Max(10f, activeMonitorRadius > 0f ? activeMonitorRadius : fallbackMonitorRadius);
            radius = Mathf.Max(radius, activeCameraPoint.MaxViewRange, 220f);
            if (hasExplicitCameraContext)
            {
                if (dist > radius) return false;
            }
            else
            {
                float expandedRadius = Mathf.Max(radius, fallbackMonitorRadius, radius * 1.5f);
                if (horizontalDist > expandedRadius) return false;
            }

            if (requireCameraForwardVisibility)
            {
                float angle = Vector3.Angle(activeCameraPoint.transform.forward, toPoint.normalized);
                float allowedAngle = Mathf.Max(110f, activeCameraPoint.FieldOfView * 1.15f);
                if (angle > allowedAngle) return false;
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
            CaptureGlobalIncidentSnapshot();

            if (cameraText != null) cameraText.text = $"CAMERA: {cctv.ActiveCameraObjectName}";
            if (intersectionText != null) intersectionText.text = $"INTERSECTION: {cctv.ActiveIntersectionName}";
            if (timerText != null) timerText.text = $"TIMER: {Mathf.CeilToInt(timer):00}s";
            if (scoreText != null) scoreText.text = $"SCORE: {Mathf.Max(0, score)}";
            if (congestionText != null) congestionText.text = $"CONGESTION: {(latestAverageCongestion * 100f):0}% ({GetCongestionStatus(latestAverageCongestion)})";
            if (flowText != null) flowText.text = $"FLOW: {(flow * 100f):0}%";
            UpdateIncidentUI(highlightCurrentCameraIncident: false);
            if (speedText != null)
            {
                float speed = freeModeManager != null && freeModeManager.IsFreeModeActive ? freeModeManager.CurrentSpeedKph : 0f;
                speedText.text = $"SPEED: {Mathf.RoundToInt(speed)} km/h";
            }
            if (cameraBadgeText != null) cameraBadgeText.text = $"CAM {Mathf.Max(1, cctv.ActiveCameraIndex + 1)}";

            if (laneListText != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"ACTIVE CAMERA: {cctv.ActiveCameraObjectName}");
                sb.AppendLine($"MONITORED LANES: {monitoredLaneSet.Count}");
                sb.AppendLine($"LOCAL COLLISIONS: {localCollisionIncidents}");
                sb.AppendLine($"GLOBAL COLLISIONS: {lastKnownGlobalCollisions}");
                sb.AppendLine();
                sb.AppendLine($"VEHICLES DETECTED: {vehiclesDetected}");
                sb.AppendLine($"WAITING VEHICLES: {waitingVehiclesDetected}");
                sb.AppendLine($"ZONE: {Mathf.RoundToInt(activeMonitorRadius)}m");
                if (enteredVehicles.Count > 0) sb.AppendLine($"ENTERED: +{enteredVehicles.Count}");
                if (exitedVehicles.Count > 0) sb.AppendLine($"LEFT: -{exitedVehicles.Count}");
                sb.AppendLine();
                if (cameraNetworkLines.Count > 0)
                {
                    sb.AppendLine("NETWORK FEED:");
                    for (int i = 0; i < cameraNetworkLines.Count; i++)
                    {
                        sb.AppendLine(cameraNetworkLines[i]);
                    }
                    sb.AppendLine();
                }

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

        private string GetIncidentGrade(int count)
        {
            if (count >= highIncidentThreshold) return "HIGH";
            if (count >= mediumIncidentThreshold) return "MEDIUM";
            return "LOW";
        }

        private static string GetFlowGrade(float flow01)
        {
            if (flow01 >= 0.85f) return "EXCELLENT";
            if (flow01 >= 0.7f) return "GOOD";
            if (flow01 >= 0.5f) return "MEDIUM";
            return "POOR";
        }

        private float GetPerformanceScore01(float flow01, int incidentCount)
        {
            float flowScore = Mathf.Clamp01(flow01);
            float incidentPenalty01;
            if (incidentCount >= highIncidentThreshold) incidentPenalty01 = 1f;
            else if (incidentCount >= mediumIncidentThreshold) incidentPenalty01 = 0.55f;
            else incidentPenalty01 = Mathf.InverseLerp(mediumIncidentThreshold, 0f, incidentCount) * 0.25f;

            // 70% flow quality + 30% incident safety.
            float safetyScore = 1f - incidentPenalty01;
            return Mathf.Clamp01(flowScore * 0.7f + safetyScore * 0.3f);
        }

        private static string GetOverallGrade(float score01)
        {
            if (score01 >= 0.9f) return "S";
            if (score01 >= 0.8f) return "A";
            if (score01 >= 0.65f) return "B";
            if (score01 >= 0.5f) return "C";
            return "D";
        }

        private void RefreshIdleUI()
        {
            if (cameraText != null && cctv != null) cameraText.text = $"CAMERA: {cctv.ActiveCameraObjectName}";
            if (intersectionText != null && cctv != null) intersectionText.text = $"INTERSECTION: {cctv.ActiveIntersectionName}";
            if (timerText != null) timerText.text = "TIMER: --";
            if (scoreText != null) scoreText.text = "SCORE: --";
            if (congestionText != null) congestionText.text = "CONGESTION: --";
            if (flowText != null) flowText.text = "FLOW: --";
            if (cameraBadgeText != null && cctv != null) cameraBadgeText.text = $"CAM {Mathf.Max(1, cctv.ActiveCameraIndex + 1)}";
            if (incidentText != null)
            {
                incidentText.text = "GLOBAL COLLISIONS: 0\nCAM 01 COLLISIONS: 0";
                incidentText.color = new Color(0.84f, 0.92f, 1f, 1f);
            }
            if (speedText != null)
            {
                float speed = freeModeManager != null && freeModeManager.IsFreeModeActive ? freeModeManager.CurrentSpeedKph : 0f;
                speedText.text = $"SPEED: {Mathf.RoundToInt(speed)} km/h";
            }
            if (laneListText != null) laneListText.text = "Press MONITOR to start a localized CCTV shift.";
            ClearVehicleMarkers();
            CaptureGlobalIncidentSnapshot();
            UpdateIncidentUI(highlightCurrentCameraIncident: false);
        }

        private void UpdateIncidentUI(bool highlightCurrentCameraIncident)
        {
            if (incidentText == null) return;
            incidentText.text =
                $"GLOBAL COLLISIONS: {lastKnownGlobalCollisions}\n" +
                $"CAM {Mathf.Max(1, cctv != null ? cctv.ActiveCameraIndex + 1 : 1):00} COLLISIONS: {localCollisionIncidents}";
            incidentText.color = highlightCurrentCameraIncident
                ? new Color(1f, 0.35f, 0.35f, 1f)
                : new Color(0.84f, 0.92f, 1f, 1f);
        }

        private void OnTrafficIncidentReported(TrafficIncidentData incident)
        {
            CaptureGlobalIncidentSnapshot();

            if (!active || activeCameraPoint == null) return;
            if (!IsIncidentInsideActiveMonitoringContext(incident)) return;

            if (incident.Type != TrafficIncidentType.Collision) return;

            localCollisionIncidents++;
            score -= 120;
            MarkCollisionIncident(incident.PrimaryVehicle);
            MarkCollisionIncident(incident.SecondaryVehicle);

            UpdateIncidentUI(highlightCurrentCameraIncident: true);
        }

        private bool IsIncidentInsideActiveMonitoringContext(TrafficIncidentData incident)
        {
            bool pointInsideMonitorArea = IsPointInsideMonitoringArea(incident.WorldPosition, incident.Lane ?? incident.OtherLane);
            bool pointInsideCameraPov = IsPointInsideActiveCameraPov(incident.WorldPosition);

            if (strictIncidentPovOnly)
            {
                return pointInsideMonitorArea && pointInsideCameraPov;
            }

            bool laneMatch =
                (incident.Lane != null && monitoredLaneSet.Contains(incident.Lane)) ||
                (incident.OtherLane != null && monitoredLaneSet.Contains(incident.OtherLane));
            bool intersectionMatch = incident.Intersection != null && monitoredIntersectionSet.Contains(incident.Intersection);
            return (laneMatch || intersectionMatch || pointInsideMonitorArea) && pointInsideCameraPov;
        }

        private bool IsPointInsideActiveCameraPov(Vector3 worldPos)
        {
            if (activeCameraPoint == null) return false;

            Camera activeViewCamera = Camera.main;
            if (activeViewCamera != null)
            {
                Vector3 vp = activeViewCamera.WorldToViewportPoint(worldPos);
                bool insideViewport = vp.z > 0.1f &&
                                      vp.x >= -0.12f && vp.x <= 1.12f &&
                                      vp.y >= -0.12f && vp.y <= 1.12f;
                if (insideViewport) return true;
            }

            Vector3 origin = activeCameraPoint.transform.position;
            Vector3 toPoint = worldPos - origin;
            float distance = toPoint.magnitude;
            if (distance <= 0.01f) return true;

            float maxDistance = Mathf.Max(10f, activeCameraPoint.MaxViewRange);
            if (distance > maxDistance) return false;

            if (Mathf.Abs(toPoint.y) > Mathf.Max(4f, activeVerticalTolerance)) return false;

            // Add a small angular tolerance so edge-of-screen collisions still count.
            float halfFov = activeCameraPoint.FieldOfView * 0.5f + 10f;
            float angle = Vector3.Angle(activeCameraPoint.transform.forward, toPoint.normalized);
            return angle <= halfFov;
        }

        private void CaptureGlobalIncidentSnapshot()
        {
            lastKnownGlobalCollisions = TrafficIncidentSystem.GlobalCollisionCount;
        }

        private void RefreshCameraNetworkFeed(bool force)
        {
            if (cctv == null) return;
            networkFeedTimer -= sampleInterval;
            if (!force && networkFeedTimer > 0f) return;
            networkFeedTimer = Mathf.Max(0.2f, networkFeedRefreshInterval);

            cameraNetworkLines.Clear();
            CCTVCameraPoint[] points = FindObjectsByType<CCTVCameraPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int shown = 0;
            for (int i = 0; i < points.Length; i++)
            {
                CCTVCameraPoint point = points[i];
                if (point == null) continue;
                int count = CountVehiclesForCamera(point);
                string marker = point == activeCameraPoint ? ">" : " ";
                cameraNetworkLines.Add($"{marker} {point.name}: {count}");
                shown++;
                if (shown >= 6) break;
            }
        }

        private int CountVehiclesForCamera(CCTVCameraPoint point)
        {
            Vector3 origin = point.transform.position;
            float radius = Mathf.Max(point.MonitorRadius, point.MaxViewRange);
            int overlapCount = Physics.OverlapSphereNonAlloc(origin, radius, networkOverlapBuffer, vehicleDetectionMask, QueryTriggerInteraction.Ignore);
            int count = 0;
            for (int i = 0; i < overlapCount; i++)
            {
                Collider col = networkOverlapBuffer[i];
                if (col == null) continue;
                TrafficCarAI car = col.GetComponentInParent<TrafficCarAI>();
                if (car != null && car.isActiveAndEnabled)
                {
                    count++;
                    continue;
                }

                Rigidbody rb = col.attachedRigidbody != null ? col.attachedRigidbody : col.GetComponentInParent<Rigidbody>();
                if (rb == null || !rb.gameObject.activeInHierarchy) continue;
                if (rb.GetComponentInParent<TrafficCarAI>() != null) continue;
                if (rb.GetComponentInParent<TrafficDetectableVehicle>() != null)
                {
                    count++;
                    continue;
                }
                if (!IsLikelyVehicleObject(rb.gameObject)) continue;
                count++;
            }
            return count;
        }

        private static bool IsLikelyVehicleObject(GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponentInChildren<WheelCollider>() != null) return true;
            if (go.GetComponent<CarAI>() != null) return true;
            if (go.GetComponent<CarWaypointFollower>() != null) return true;

            string n = go.name.ToLowerInvariant();
            return n.Contains("car") || n.Contains("vehicle") || n.Contains("taxi") || n.Contains("bus") || n.Contains("truck");
        }

        private void UpdateVehicleMarkers()
        {
            if (!showDetectionStateOnCars) return;
            PruneCollisionHighlights();

            foreach (TrafficCarAI car in trackedVehicles)
            {
                if (car == null) continue;
                if (!markerByVehicle.TryGetValue(car, out GameObject marker) || marker == null)
                {
                    marker = CreateVehicleMarker(car.transform);
                    markerByVehicle[car] = marker;
                }

                UpdateVehicleMarkerShape(marker, car);
                Color c = (IsCollisionHighlighted(car) || IsCarOffExpectedPath(car)) ? alertVehicleColor : movingVehicleColor;
                ApplyMarkerColor(marker, c);
            }

            List<TrafficCarAI> toRemove = new List<TrafficCarAI>();
            foreach (KeyValuePair<TrafficCarAI, GameObject> kv in markerByVehicle)
            {
                if (kv.Key == null || !trackedVehicles.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            for (int i = 0; i < toRemove.Count; i++) markerByVehicle.Remove(toRemove[i]);
        }

        private static GameObject CreateVehicleMarker(Transform parentCar)
        {
            GameObject root = new GameObject("DetectionMarker");
            root.transform.SetParent(parentCar, true);

            for (int i = 0; i < 4; i++)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"Strip_{i + 1}";
                strip.transform.SetParent(root.transform, false);
                strip.transform.localScale = new Vector3(0.08f, 0.02f, 0.55f);
                if (strip.TryGetComponent<Collider>(out Collider col)) Destroy(col);
                Renderer r = strip.GetComponent<Renderer>();
                if (r != null)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    Material m = new Material(Shader.Find("Standard"));
                    m.EnableKeyword("_EMISSION");
                    r.sharedMaterial = m;
                }
            }

            return root;
        }

        private static void UpdateVehicleMarkerShape(GameObject marker, TrafficCarAI car)
        {
            if (marker == null || car == null) return;

            Bounds b = new Bounds(car.transform.position + Vector3.up * 0.7f, new Vector3(1.4f, 0.1f, 2.8f));
            Collider col = car.GetComponent<Collider>();
            if (col != null) b = col.bounds;

            float y = b.max.y + 0.08f;
            marker.transform.position = new Vector3(b.center.x, y, b.center.z);
            marker.transform.rotation = Quaternion.Euler(0f, car.transform.eulerAngles.y, 0f);

            float halfWidth = Mathf.Max(0.28f, b.extents.x);
            float zSize = Mathf.Max(0.6f, b.extents.z * 0.95f);
            for (int i = 0; i < marker.transform.childCount; i++)
            {
                Transform strip = marker.transform.GetChild(i);
                if (strip == null) continue;
                float t = i / 3f;
                float x = Mathf.Lerp(-halfWidth, halfWidth, t);
                strip.localPosition = new Vector3(x, 0f, 0f);
                strip.localScale = new Vector3(0.075f, 0.02f, zSize);
            }
        }

        private static void ApplyMarkerColor(GameObject marker, Color c)
        {
            if (marker == null) return;
            for (int i = 0; i < marker.transform.childCount; i++)
            {
                Renderer r = marker.transform.GetChild(i).GetComponent<Renderer>();
                if (r == null || r.sharedMaterial == null) continue;
                r.sharedMaterial.color = c;
                r.sharedMaterial.SetColor("_EmissionColor", c * 1.85f);
            }
        }

        private void ClearVehicleMarkers()
        {
            foreach (KeyValuePair<TrafficCarAI, GameObject> kv in markerByVehicle)
            {
                if (kv.Value != null) Destroy(kv.Value);
            }
            markerByVehicle.Clear();
            waitingVehicles.Clear();
            collisionIncidentUntil.Clear();
        }

        private void MarkCollisionIncident(TrafficCarAI car)
        {
            if (car == null) return;
            collisionIncidentUntil[car] = Time.time + Mathf.Max(0.25f, incidentHighlightDuration);
        }

        private bool IsCollisionHighlighted(TrafficCarAI car)
        {
            if (car == null) return false;
            if (!collisionIncidentUntil.TryGetValue(car, out float until)) return false;
            return Time.time <= until;
        }

        private void PruneCollisionHighlights()
        {
            if (collisionIncidentUntil.Count == 0) return;
            List<TrafficCarAI> expired = null;
            foreach (KeyValuePair<TrafficCarAI, float> kv in collisionIncidentUntil)
            {
                if (kv.Key == null || Time.time > kv.Value)
                {
                    if (expired == null) expired = new List<TrafficCarAI>();
                    expired.Add(kv.Key);
                }
            }

            if (expired == null) return;
            for (int i = 0; i < expired.Count; i++)
            {
                collisionIncidentUntil.Remove(expired[i]);
            }
        }

        private bool IsCarOffExpectedPath(TrafficCarAI car)
        {
            if (car == null) return false;
            Lane lane = car.CurrentLane;
            if (lane == null) return true;

            var waypoints = lane.Waypoints;
            if (waypoints == null || waypoints.Count == 0) return true;

            int idx = Mathf.Clamp(car.CurrentWaypointIndex, 0, waypoints.Count - 1);
            Vector3 carPos = car.transform.position;
            Vector3 target = waypoints[idx] != null ? waypoints[idx].transform.position : lane.transform.position;

            float distToTarget = Vector3.Distance(carPos, target);
            if (distToTarget <= Mathf.Max(2f, pathDeviationThreshold)) return false;

            if (idx > 0 && waypoints[idx - 1] != null && waypoints[idx] != null)
            {
                Vector3 a = waypoints[idx - 1].transform.position;
                Vector3 b = waypoints[idx].transform.position;
                float segmentDist = DistancePointToSegmentXZ(carPos, a, b);
                return segmentDist > Mathf.Max(2f, pathDeviationThreshold);
            }

            return true;
        }

        private static float DistancePointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 bb = new Vector2(b.x, b.z);
            Vector2 ab = bb - aa;
            float denom = Vector2.Dot(ab, ab);
            if (denom <= 0.0001f) return Vector2.Distance(pp, aa);
            float t = Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / denom);
            Vector2 proj = aa + ab * t;
            return Vector2.Distance(pp, proj);
        }

        private void BuildUIIfMissing()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }

            monitorButton = null;
            freeRoamButton = null;
            cameraText = null;
            intersectionText = null;
            timerText = null;
            scoreText = null;
            congestionText = null;
            flowText = null;
            incidentText = null;
            speedText = null;
            laneListText = null;
            cameraBadgeText = null;
            resultText = null;
            resultGroup = null;
            restartButton = null;

            canvas = new GameObject("CameraChallengeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Transform root = canvas.transform;
            Image topLeft = Panel(root, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(260f, -105f), new Vector2(500f, 190f));
            Image topRight = Panel(root, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-220f, -100f), new Vector2(450f, 170f));
            Image speedPanel = Panel(root, "SpeedPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(320f, 90f));
            Image left = Panel(root, "LeftPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(250f, -20f), new Vector2(540f, 500f));
            Image centerBottom = Panel(root, "CenterBottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(620f, 90f));
            Image camBadgePanel = Panel(root, "CameraBadgePanel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(130f, 55f), new Vector2(210f, 58f));
            resultGroup = Overlay(root, "ResultOverlay");
            resultText = Label(resultGroup.transform, "ResultText", "", new Vector2(0f, 0f), 44f, true);
            restartButton = CreateButton(resultGroup.transform, "RESTART", new Vector2(0f, -180f), new Vector2(280f, 62f));

            cameraText = Label(topLeft.transform, "CameraText", "CAMERA: --", new Vector2(0f, 60f), 26f, false);
            intersectionText = Label(topLeft.transform, "IntersectionText", "INTERSECTION: --", new Vector2(0f, 20f), 24f, false);
            timerText = Label(topLeft.transform, "TimerText", "TIMER: --", new Vector2(0f, -20f), 24f, false);
            scoreText = Label(topLeft.transform, "ScoreText", "SCORE: --", new Vector2(0f, -60f), 24f, false);

            congestionText = Label(topRight.transform, "CongText", "CONGESTION: --", new Vector2(0f, 40f), 24f, false);
            flowText = Label(topRight.transform, "FlowText", "FLOW: --", new Vector2(0f, 0f), 24f, false);
            incidentText = Label(topRight.transform, "IncidentText", "GLOBAL COLLISIONS: 0\nCAM 01 COLLISIONS: 0", new Vector2(0f, -40f), 24f, false);
            speedText = Label(speedPanel.transform, "SpeedText", "SPEED: 0 km/h", new Vector2(0f, 0f), 34f, true);
            speedText.fontStyle = FontStyles.Bold;

            laneListText = Label(left.transform, "LaneListText", "", new Vector2(0f, 0f), 22f, false);
            laneListText.alignment = TextAlignmentOptions.TopLeft;
            laneListText.rectTransform.sizeDelta = new Vector2(500f, 450f);
            cameraBadgeText = Label(camBadgePanel.transform, "CameraBadgeText", "CAM 1", new Vector2(0f, 0f), 28f, true);
            cameraBadgeText.characterSpacing = 2.5f;
            cameraBadgeText.fontStyle = FontStyles.Bold;

            monitorButton = CreateButton(centerBottom.transform, "MONITOR", new Vector2(-150f, 0f), new Vector2(250f, 54f));
            freeRoamButton = CreateButton(centerBottom.transform, "FREE ROAM", new Vector2(150f, 0f), new Vector2(250f, 54f));
            HideResult();
        }

        public bool EnsureReady(bool log = true)
        {
            if (cctv == null) cctv = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            if (masterLights == null) masterLights = FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include);
            if (freeModeManager == null) freeModeManager = FindFirstObjectByType<FreeModeManager>(FindObjectsInactive.Include);
            EnsureEventSystem();

            if (canvas == null || monitorButton == null || freeRoamButton == null)
            {
                BuildUIIfMissing();
            }
            WireUI();
            RefreshIdleUI();

            isReady = cctv != null && monitorButton != null && freeRoamButton != null;
            if (log)
            {
                if (isReady && !hasLoggedReady)
                {
                    Debug.Log($"[OK] {nameof(CameraChallengeManager)} initialized");
                    hasLoggedReady = true;
                }
                else if (!isReady)
                {
                    Debug.LogWarning($"[WARN] {nameof(CameraChallengeManager)} missing refs. CCTV={cctv != null}, MonitorBtn={monitorButton != null}, FreeBtn={freeRoamButton != null}");
                }
            }

            return isReady;
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (es != null)
            {
                if (es.GetComponent<BaseInputModule>() == null)
                {
                    es.gameObject.AddComponent<StandaloneInputModule>();
                }
                return;
            }

            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void WireUI()
        {
            if (monitorButton != null)
            {
                monitorButton.onClick.RemoveAllListeners();
                monitorButton.onClick.AddListener(StartCurrentCameraChallenge);
            }
            if (freeRoamButton != null)
            {
                freeRoamButton.onClick.RemoveAllListeners();
                freeRoamButton.onClick.AddListener(EnterFreeRoamMode);
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(RestartGameplayScene);
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

        private void RestartGameplayScene()
        {
            string scene = SceneManager.GetActiveScene().name;
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
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
