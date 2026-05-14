using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.TrafficLights
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrafficLightController))]
    public class TrafficLightWorldLabel : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 3.2f, 0f);
        [SerializeField] private float minScale = 0.9f;
        [SerializeField] private float maxScale = 1.25f;
        [SerializeField] private float scaleDistance = 45f;

        private TrafficLightController controller;
        private TrafficLightGroup group;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform root;
        private TextMeshProUGUI stateText;
        private TextMeshProUGUI infoText;
        private Camera cam;
        private float refreshTimer;

        private void Awake()
        {
            controller = GetComponent<TrafficLightController>();
            group = GetComponentInParent<TrafficLightGroup>();
            CreateLabelObjects();
        }

        private void Update()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            bool visible = TrafficLightDebugSettings.ShowTrafficLightDebugInfo && TrafficLightDebugSettings.ShowWorldLabels;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;

            if (!visible || cam == null)
            {
                return;
            }

            transformLabel();

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = Mathf.Max(0.05f, TrafficLightDebugSettings.LabelUpdateInterval);
                RefreshText();
            }
        }

        private void transformLabel()
        {
            root.position = transform.position + worldOffset;
            Vector3 forward = root.position - cam.transform.position;
            if (forward.sqrMagnitude > 0.01f)
            {
                root.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            float dist = Vector3.Distance(root.position, cam.transform.position);
            float t = Mathf.Clamp01(dist / Mathf.Max(1f, scaleDistance));
            float scale = Mathf.Lerp(maxScale, minScale, t);
            root.localScale = Vector3.one * (0.01f * scale);
        }

        private void RefreshText()
        {
            TrafficLightState state = controller.CurrentState;
            stateText.text = $"[ {state.ToString().ToUpperInvariant()} ]";
            stateText.color = state == TrafficLightState.Green ? new Color(0.45f, 1f, 0.55f, 1f) :
                              state == TrafficLightState.Red ? new Color(1f, 0.4f, 0.4f, 1f) :
                              new Color(1f, 0.82f, 0.24f, 1f);

            if (!TrafficLightDebugSettings.ShowExtraInfo)
            {
                infoText.text = $"Key: {controller.KeyboardToggleKey}";
                return;
            }

            string groupName = group != null ? group.GroupName : "No Group";
            string autoMode = controller.AutoCycleEnabled ? "ON" : "OFF";
            infoText.text =
                $"Key: {controller.KeyboardToggleKey}\\n" +
                $"Auto: {autoMode}  Timer: {controller.RemainingTimer:0.0}s\\n" +
                $"Name: {gameObject.name}  Group: {groupName}";
        }

        private void CreateLabelObjects()
        {
            GameObject canvasObj = new GameObject("TrafficLightWorldLabel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObj.transform.SetParent(transform, false);

            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1200;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 18f;

            canvasGroup = canvasObj.GetComponent<CanvasGroup>();
            root = canvasObj.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(360f, 130f);

            GameObject bgObj = new GameObject("Background", typeof(Image));
            bgObj.transform.SetParent(root, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bg = bgObj.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.12f, 0.72f);

            stateText = CreateText("State", root, 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -8f), new Vector2(0f, 46f));
            infoText = CreateText("Info", root, 18f, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, -50f), new Vector2(0f, 84f));
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, float size, FontStyles style, TextAlignmentOptions align, Vector2 topOffset, Vector2 height)
        {
            GameObject go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = new Color(0.93f, 0.96f, 1f, 1f);
            tmp.enableWordWrapping = false;

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = topOffset;
            rt.sizeDelta = new Vector2(0f, height.y);
            return tmp;
        }
    }
}
