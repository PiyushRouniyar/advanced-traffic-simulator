using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    public class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 7f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float scanInterval = 0.2f;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2.6f, 0f);

        private Camera mainCamera;
        private TrafficLightInteractable nearest;
        private TrafficPromptUI prompt;
        private TrafficLightUIController panel;
        private float scanTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureEventSystem();
            EnsureCameraControllers();
            EnsureLightInteractables();
            EnsureVehicleRuleHandlers();
        }

        private static void EnsureCameraControllers()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            if (cam.GetComponent<PlayerInteractionController>() == null)
            {
                cam.gameObject.AddComponent<PlayerInteractionController>();
            }
        }

        private static void EnsureLightInteractables()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i] == null)
                {
                    continue;
                }

                if (!allObjects[i].name.StartsWith("Traffic_light", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (allObjects[i].GetComponent<MyTrafficSystem.TrafficLights.TrafficLightController>() == null)
                {
                    allObjects[i].AddComponent<MyTrafficSystem.TrafficLights.TrafficLightController>();
                }
            }

            MyTrafficSystem.TrafficLights.TrafficLightController[] lights = FindObjectsByType<MyTrafficSystem.TrafficLights.TrafficLightController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                if (lights[i].GetComponent<TrafficLightInteractable>() == null)
                {
                    lights[i].gameObject.AddComponent<TrafficLightInteractable>();
                }
            }
        }

        private static void EnsureVehicleRuleHandlers()
        {
            MyTrafficSystem.AI.TrafficCarAI[] cars = FindObjectsByType<MyTrafficSystem.AI.TrafficCarAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i] != null && cars[i].GetComponent<VehicleTrafficRuleHandler>() == null)
                {
                    cars[i].gameObject.AddComponent<VehicleTrafficRuleHandler>();
                }
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void Awake()
        {
            mainCamera = Camera.main != null ? Camera.main : GetComponent<Camera>();
            panel = FindFirstObjectByType<TrafficLightUIController>(FindObjectsInactive.Include);
            if (panel == null)
            {
                panel = TrafficLightUIFactory.CreatePanelUI();
            }

            panel.onTurnGreen.AddListener(() => nearest?.TurnGreen());
            panel.onTurnRed.AddListener(() => nearest?.TurnRed());
            panel.onClose.AddListener(() =>
            {
                LockCursor(true);
                if (nearest != null) nearest.SetHighlighted(true);
            });
        }

        private void Update()
        {
            scanTimer -= Time.unscaledDeltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = scanInterval;
                ScanNearest();
            }

            UpdatePrompt();

            if (panel != null && panel.IsOpen)
            {
                return;
            }

            if (nearest != null && Input.GetKeyDown(interactKey))
            {
                panel.Open(nearest);
                LockCursor(false);
            }
        }

        private void ScanNearest()
        {
            TrafficLightInteractable old = nearest;
            TrafficLightInteractable[] all = FindObjectsByType<TrafficLightInteractable>(FindObjectsSortMode.None);
            float best = interactionRange * interactionRange;
            nearest = null;

            Vector3 from = transform.position;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                {
                    continue;
                }

                float sqr = (all[i].transform.position - from).sqrMagnitude;
                if (sqr < best)
                {
                    best = sqr;
                    nearest = all[i];
                }
            }

            if (old != null && old != nearest) old.SetHighlighted(false);
            if (nearest != null) nearest.SetHighlighted(true);
        }

        private void UpdatePrompt()
        {
            bool show = nearest != null && (panel == null || !panel.IsOpen);
            if (!show)
            {
                if (prompt != null) prompt.SetVisible(false);
                return;
            }

            if (prompt == null)
            {
                prompt = TrafficLightUIFactory.CreateWorldPrompt(mainCamera);
            }

            prompt.transform.position = nearest.transform.position + promptOffset;
            prompt.BindCamera(mainCamera);
            prompt.SetText("[ Press E to Control Traffic ]");
            prompt.SetVisible(true);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }

    internal static class TrafficLightUIFactory
    {
        public static TrafficPromptUI CreateWorldPrompt(Camera cam)
        {
            GameObject root = new GameObject("TrafficPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 500;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(340f, 80f);
            rootRect.localScale = Vector3.one * 0.01f;

            GameObject bg = new GameObject("PromptBg", typeof(Image), typeof(CanvasGroup));
            bg.transform.SetParent(root.transform, false);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(320f, 58f);
            Image bgi = bg.GetComponent<Image>();
            bgi.color = new Color(0.08f, 0.12f, 0.18f, 0.82f);

            GameObject txt = new GameObject("Text", typeof(TextMeshProUGUI));
            txt.transform.SetParent(bg.transform, false);
            RectTransform txtr = txt.GetComponent<RectTransform>();
            txtr.anchorMin = Vector2.zero;
            txtr.anchorMax = Vector2.one;
            txtr.offsetMin = new Vector2(8, 8);
            txtr.offsetMax = new Vector2(-8, -8);
            TextMeshProUGUI label = txt.GetComponent<TextMeshProUGUI>();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.92f, 0.96f, 1f, 1f);

            TrafficPromptUI p = bg.AddComponent<TrafficPromptUI>();
            p.Initialize(bg.GetComponent<CanvasGroup>(), label);
            p.BindCamera(cam);
            return p;
        }

        public static TrafficLightUIController CreatePanelUI()
        {
            GameObject canvasObj = new GameObject("TrafficControlCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();

            Image dim = Create(canvasObj.transform, "Dim", typeof(Image)).GetComponent<Image>();
            RectTransform dimRt = dim.rectTransform;
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dim.color = new Color(0.01f, 0.02f, 0.04f, 0f);

            Image panel = Create(canvasObj.transform, "Panel", typeof(Image)).GetComponent<Image>();
            RectTransform pr = panel.rectTransform;
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(580f, 420f);
            panel.color = new Color(0.08f, 0.11f, 0.16f, 0.9f);

            TextMeshProUGUI title = Text(panel.transform, "Title", "Traffic Controller", 40f, FontStyles.Bold, new Vector2(24f, -24f));
            TextMeshProUGUI name = Text(panel.transform, "Name", "Light: --", 26f, FontStyles.Normal, new Vector2(24f, -82f));
            TextMeshProUGUI state = Text(panel.transform, "State", "Current State: --", 24f, FontStyles.Normal, new Vector2(24f, -130f));
            TextMeshProUGUI status = Text(panel.transform, "Status", "Nearby Traffic: --", 24f, FontStyles.Normal, new Vector2(24f, -170f));
            TextMeshProUGUI mode = Text(panel.transform, "Mode", "Current Mode: Manual", 24f, FontStyles.Normal, new Vector2(24f, -210f));

            Button green = Button(panel.transform, "TURN GREEN", new Vector2(0f, 106f), new Color(0.2f, 0.68f, 0.42f, 0.96f));
            Button red = Button(panel.transform, "TURN RED", new Vector2(0f, 52f), new Color(0.78f, 0.25f, 0.2f, 0.96f));
            Button close = Button(panel.transform, "CLOSE", new Vector2(0f, 8f), new Color(0.34f, 0.36f, 0.44f, 0.96f));

            green.gameObject.AddComponent<TrafficUIButtonFeedback>();
            red.gameObject.AddComponent<TrafficUIButtonFeedback>();
            close.gameObject.AddComponent<TrafficUIButtonFeedback>();

            TrafficLightUIController ui = canvasObj.AddComponent<TrafficLightUIController>();
            ui.Initialize(cg, pr, dim, title, name, state, status, mode, green, red, close);
            return ui;
        }

        private static GameObject Create(Transform parent, string name, params System.Type[] components)
        {
            GameObject go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, float size, FontStyles style, Vector2 anchored)
        {
            GameObject go = Create(parent, name, typeof(TextMeshProUGUI));
            TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = value;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = TextAlignmentOptions.Left;
            txt.color = new Color(0.95f, 0.97f, 1f, 1f);

            RectTransform rt = txt.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(-24f, 36f);
            return txt;
        }

        private static Button Button(Transform parent, string label, Vector2 anchored, Color color)
        {
            GameObject go = Create(parent, label, typeof(Image), typeof(Button));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(500f, 44f);
            rt.anchoredPosition = anchored;
            Image img = go.GetComponent<Image>();
            img.color = color;

            TextMeshProUGUI txt = Text(go.transform, "Label", label, 24f, FontStyles.Normal, new Vector2(0f, 0f));
            RectTransform tr = txt.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            txt.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }
    }
}
