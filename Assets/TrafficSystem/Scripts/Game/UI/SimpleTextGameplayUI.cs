using System.Collections.Generic;
using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.Gameplay.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay.UI
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public class SimpleTextGameplayUI : MonoBehaviour
    {
        [SerializeField] private CCTVCameraSystem cctv;
        [SerializeField] private TrafficGameManager gameManager;
        [SerializeField] private TrafficCongestionMonitor congestion;
        [SerializeField] private TrafficScoreSystem scoreSystem;
        [SerializeField] private MasterTrafficLightController masterLightController;

        private TextMeshProUGUI cameraText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI scoreText;

        private TextMeshProUGUI congestionText;
        private TextMeshProUGUI flowText;
        private TextMeshProUGUI pedestrianText;

        private Slider congestionSlider;
        private Slider flowSlider;
        private Slider pedestrianSlider;

        private readonly List<Button> cameraButtons = new List<Button>();
        private Button nsButton;
        private Button ewButton;
        private Button crosswalkButton;
        private int activeTrafficMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<SimpleTextGameplayUI>(FindObjectsInactive.Include) != null) return;
            GameObject go = new GameObject("SimpleTextGameplayUI");
            go.AddComponent<SimpleTextGameplayUI>();
        }

        private void Start()
        {
            ResolveSystems();
            BuildUI();
            BuildCameraButtons();
            HookTrafficButtons();
            RefreshTrafficButtonVisuals();
        }

        private void Update()
        {
            if (cctv == null || congestion == null || scoreSystem == null) ResolveSystems();

            if (cctv != null)
            {
                cameraText.text = $"CAMERA: {cctv.ActiveCameraLabel}";
                UpdateCameraButtonHighlights();
            }

            if (gameManager != null)
            {
                timerText.text = $"STATE: {gameManager.State}";
            }

            if (scoreSystem != null)
            {
                scoreText.text = $"SCORE: {scoreSystem.CurrentScore}";
                flowText.text = $"FLOW: {(scoreSystem.FlowEfficiency01 * 100f):0}%";
                pedestrianText.text = $"PEDESTRIAN SAFETY: {(scoreSystem.PedestrianSafety01 * 100f):0}%";

                if (flowSlider != null) flowSlider.value = scoreSystem.FlowEfficiency01;
                if (pedestrianSlider != null) pedestrianSlider.value = scoreSystem.PedestrianSafety01;
            }

            if (congestion != null)
            {
                congestionText.text = $"CONGESTION: {(congestion.NormalizedCongestion * 100f):0}%";
                if (congestionSlider != null) congestionSlider.value = congestion.NormalizedCongestion;
            }
        }

        private void ResolveSystems()
        {
            if (cctv == null) cctv = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            if (gameManager == null) gameManager = FindFirstObjectByType<TrafficGameManager>(FindObjectsInactive.Include);
            if (congestion == null) congestion = FindFirstObjectByType<TrafficCongestionMonitor>(FindObjectsInactive.Include);
            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<TrafficScoreSystem>(FindObjectsInactive.Include);
            if (masterLightController == null) masterLightController = FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include);
        }

        private void BuildUI()
        {
            Canvas canvas = new GameObject("SimpleGameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            DontDestroyOnLoad(canvas.gameObject);

            Transform root = canvas.transform;
            Image topLeft = Panel(root, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(245f, -90f), new Vector2(470f, 170f));
            Image topRight = Panel(root, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-255f, -130f), new Vector2(500f, 250f));
            Image bottomLeft = Panel(root, "BottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(260f, 110f), new Vector2(500f, 180f));
            Image bottomCenter = Panel(root, "BottomCenter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(640f, 140f));

            cameraText = Label(topLeft.transform, "Camera", "CAMERA: --", new Vector2(0f, 40f));
            timerText = Label(topLeft.transform, "Timer", "STATE: --", new Vector2(0f, 0f));
            scoreText = Label(topLeft.transform, "Score", "SCORE: 0", new Vector2(0f, -40f));

            congestionText = Label(topRight.transform, "Congestion", "CONGESTION: 0%", new Vector2(0f, 70f));
            flowText = Label(topRight.transform, "Flow", "FLOW: 0%", new Vector2(0f, 15f));
            pedestrianText = Label(topRight.transform, "Pedestrian", "PEDESTRIAN SAFETY: 0%", new Vector2(0f, -40f));

            congestionSlider = SliderBar(topRight.transform, "CongestionBar", new Vector2(0f, 45f), new Color(1f, 0.35f, 0.35f, 1f));
            flowSlider = SliderBar(topRight.transform, "FlowBar", new Vector2(0f, -10f), new Color(0.35f, 1f, 0.65f, 1f));
            pedestrianSlider = SliderBar(topRight.transform, "PedBar", new Vector2(0f, -65f), new Color(0.35f, 0.95f, 1f, 1f));

            Label(bottomLeft.transform, "CamSwitchTitle", "CAMERA SWITCH", new Vector2(0f, 62f));
            Label(bottomCenter.transform, "TrafficTitle", "TRAFFIC CONTROL", new Vector2(0f, 42f));

            CreateTrafficButtons(bottomCenter.transform);
            CreateCameraButtonsContainer(bottomLeft.transform);
        }

        private void CreateCameraButtonsContainer(Transform parent)
        {
            GameObject row = new GameObject("CameraButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(460f, 80f);

            HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
        }

        private void BuildCameraButtons()
        {
            cameraButtons.Clear();
            if (cctv == null) return;

            Transform container = GameObject.Find("CameraButtons")?.transform;
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }

            int count = Mathf.Min(8, cctv.CameraCount);
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                Button btn = ButtonItem(container, cctv.GetCameraLabel(i));
                btn.onClick.AddListener(() => cctv.SetActiveCamera(idx));
                cameraButtons.Add(btn);
            }
        }

        private void UpdateCameraButtonHighlights()
        {
            for (int i = 0; i < cameraButtons.Count; i++)
            {
                if (cameraButtons[i] == null) continue;
                Image img = cameraButtons[i].GetComponent<Image>();
                if (img == null) continue;
                bool active = cctv != null && i == cctv.ActiveCameraIndex;
                img.color = active ? new Color(0.2f, 0.75f, 0.95f, 0.95f) : new Color(0.12f, 0.2f, 0.28f, 0.95f);
            }
        }

        private void CreateTrafficButtons(Transform parent)
        {
            GameObject row = new GameObject("TrafficButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -18f);
            rt.sizeDelta = new Vector2(600f, 70f);

            HorizontalLayoutGroup h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            nsButton = ButtonItem(row.transform, "North/South Green");
            ewButton = ButtonItem(row.transform, "East/West Green");
            crosswalkButton = ButtonItem(row.transform, "Crosswalk");
        }

        private void HookTrafficButtons()
        {
            nsButton?.onClick.AddListener(() =>
            {
                ApplyPhaseToAll(0);
                activeTrafficMode = 0;
                RefreshTrafficButtonVisuals();
            });

            ewButton?.onClick.AddListener(() =>
            {
                ApplyPhaseToAll(1);
                activeTrafficMode = 1;
                RefreshTrafficButtonVisuals();
            });

            crosswalkButton?.onClick.AddListener(() =>
            {
                if (masterLightController != null) masterLightController.SetAllRed();
                activeTrafficMode = 2;
                RefreshTrafficButtonVisuals();
            });
        }

        private void ApplyPhaseToAll(int phaseIndex)
        {
            IntersectionController[] controllers = FindObjectsByType<IntersectionController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null) controllers[i].SetPhase(phaseIndex);
            }
        }

        private void RefreshTrafficButtonVisuals()
        {
            SetButtonState(nsButton, activeTrafficMode == 0);
            SetButtonState(ewButton, activeTrafficMode == 1);
            SetButtonState(crosswalkButton, activeTrafficMode == 2);
        }

        private static void SetButtonState(Button btn, bool active)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img == null) return;
            img.color = active ? new Color(0.32f, 0.88f, 0.58f, 0.95f) : new Color(0.14f, 0.2f, 0.25f, 0.95f);
        }

        private static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.06f, 0.08f, 0.11f, 0.88f);
            return img;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string value, Vector2 pos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(460f, 34f);

            TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = value;
            txt.fontSize = 24f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.82f, 0.94f, 1f, 1f);
            return txt;
        }

        private static Slider SliderBar(Transform parent, string name, Vector2 pos, Color fillColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(420f, 14f);

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.2f, 1f);

            GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = new Vector2(2f, 2f);
            faRt.offsetMax = new Vector2(-2f, -2f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = fill.GetComponent<Image>();
            fillImg.color = fillColor;

            Slider slider = root.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            return slider;
        }

        private static Button ButtonItem(Transform parent, string label)
        {
            GameObject go = new GameObject(label.Replace(" ", "_") + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.14f, 0.2f, 0.25f, 0.95f);

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.22f, 0.35f, 0.42f, 1f);
            cb.pressedColor = new Color(0.1f, 0.58f, 0.78f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(go.transform, false);
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 20f;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.88f, 0.96f, 1f, 1f);

            return btn;
        }
    }
}
