using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MyTrafficSystem.TrafficLights;
using System.IO;

namespace MyTrafficSystem.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class FuturisticMainMenuController : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private int gameplaySceneBuildIndex = -1;
        [SerializeField] private float transitionDuration = 0.85f;
        [SerializeField] private bool forceStartFirstLevelAfterLoad = true;

        [Header("Branding")]
        [SerializeField] private string gameTitleLine1 = "URBAN";
        [SerializeField] private string gameTitleLine2 = "FLOW";
        [SerializeField] private string gameSubtitle = "SMART CITY TRAFFIC CONTROL";

        [Header("Palette")]
        [SerializeField] private Color bgTop = new Color(0.19f, 0.16f, 0.11f, 1f);
        [SerializeField] private Color bgBottom = new Color(0.04f, 0.045f, 0.04f, 1f);
        [SerializeField] private Color accent = new Color(0.96f, 0.78f, 0.45f, 1f);
        [SerializeField] private Color accentSoft = new Color(1f, 0.82f, 0.54f, 0.58f);
        [SerializeField] private Color panelTint = new Color(0.03f, 0.03f, 0.03f, 0.26f);

        private Canvas rootCanvas;
        private CanvasGroup fadeGroup;
        private RawImage bgGradient;
        private RawImage bgGrid;
        private RectTransform panelRect;
        private bool loading;

        private void Awake()
        {
            BuildIfMissing();
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            if (bgGrid != null)
            {
                bgGrid.uvRect = new Rect(t * 0.0035f, t * -0.0065f, 1f, 1f);
                Color c = bgGrid.color;
                c.a = 0.08f + Mathf.PingPong(t * 0.05f, 0.04f);
                bgGrid.color = c;
            }

            if (panelRect != null)
            {
                float y = Mathf.Sin(t * 0.65f) * 2f;
                panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, -185f + y);
            }
        }

        public void StartGame()
        {
            if (loading) return;
            StartCoroutine(StartGameRoutine());
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator StartGameRoutine()
        {
            loading = true;
            if (fadeGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < transitionDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeGroup.alpha = Mathf.Clamp01(elapsed / transitionDuration);
                    yield return null;
                }
                fadeGroup.alpha = 1f;
            }

            Time.timeScale = 1f;
            AsyncOperation load = TryCreateSceneLoadOperation();
            if (load == null)
            {
                Debug.LogError($"[MainMenu] Could not load gameplay scene. Set a valid Gameplay Scene Name or Build Index on {nameof(FuturisticMainMenuController)}.");
                loading = false;
                if (fadeGroup != null) fadeGroup.alpha = 0f;
                yield break;
            }

            while (!load.isDone)
            {
                yield return null;
            }

            // Safety net: ensure gameplay starts even if manager auto-start is disabled in inspector.
            if (forceStartFirstLevelAfterLoad)
            {
                yield return null;
                MyTrafficSystem.Gameplay.TrafficGameManager gameManager =
                    Object.FindFirstObjectByType<MyTrafficSystem.Gameplay.TrafficGameManager>(FindObjectsInactive.Include);
                if (gameManager != null)
                {
                    gameManager.StartLevel(0);
                }
            }

            EnsureTrafficLightWorldLabelsVisible();
        }

        private AsyncOperation TryCreateSceneLoadOperation()
        {
            if (gameplaySceneBuildIndex >= 0 && gameplaySceneBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                return SceneManager.LoadSceneAsync(gameplaySceneBuildIndex);
            }

            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    if (string.Equals(fileName, gameplaySceneName, System.StringComparison.Ordinal))
                    {
                        return SceneManager.LoadSceneAsync(gameplaySceneName);
                    }
                }
            }

            return null;
        }

        private void BuildIfMissing()
        {
            EnsureEventSystem();

            rootCanvas = GetComponentInChildren<Canvas>(true);
            if (rootCanvas != null) return;

            GameObject canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 2000;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Texture2D gradientTex = MakeVerticalGradient(bgBottom, bgTop);
            Texture2D gridTex = MakeGridTexture(new Color(1f, 0.8f, 0.5f, 0.2f), new Color(0f, 0f, 0f, 0f));

            bgGradient = MakeRawImage("BackgroundGradient", rootCanvas.transform as RectTransform, gradientTex, Color.white);
            bgGrid = MakeRawImage("BackgroundGrid", rootCanvas.transform as RectTransform, gridTex, new Color(1f, 0.78f, 0.45f, 0.12f));

            Image vignette = MakeImage("Vignette", rootCanvas.transform as RectTransform, new Color(0f, 0f, 0f, 0.4f));
            vignette.rectTransform.sizeDelta = Vector2.zero;

            RectTransform titleRoot = NewRect("TitleRoot", rootCanvas.transform as RectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(-40f, 0f), new Vector2(860f, 260f));
            TextMeshProUGUI title = MakeText("Title", titleRoot, $"{gameTitleLine1}\n{gameTitleLine2}", 116f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.75f, 1f));
            title.enableAutoSizing = true;
            title.fontSizeMin = 68f;
            title.fontSizeMax = 120f;
            title.outlineWidth = 0.12f;
            title.outlineColor = new Color(0.22f, 0.15f, 0.07f, 0.85f);
            title.characterSpacing = 2f;

            TextMeshProUGUI subtitle = MakeText("Subtitle", titleRoot, gameSubtitle, 26f, FontStyles.Bold, TextAlignmentOptions.Center, accentSoft);
            subtitle.rectTransform.anchoredPosition = new Vector2(0f, -122f);
            subtitle.characterSpacing = 5.2f;

            panelRect = NewRect("GlassPanel", rootCanvas.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -185f), new Vector2(420f, 250f));
            Image panel = panelRect.gameObject.AddComponent<Image>();
            panel.color = panelTint;

            Outline panelOutline = panelRect.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(accent.r, accent.g, accent.b, 0.18f);
            panelOutline.effectDistance = new Vector2(1.2f, 1.2f);

            TextMeshProUGUI controlsTitle = MakeText("ControlsTitle", panelRect, "TRAFFIC CONTROL", 24f, FontStyles.Bold, TextAlignmentOptions.Center, accentSoft);
            controlsTitle.rectTransform.anchoredPosition = new Vector2(0f, 78f);
            controlsTitle.characterSpacing = 3f;
            controlsTitle.color = new Color(accent.r, accent.g, accent.b, 0.42f);

            Button startButton = MakeButton(panelRect, "START GAME", new Vector2(0f, 22f));
            Button quitButton = MakeButton(panelRect, "QUIT", new Vector2(0f, -58f));

            startButton.onClick.AddListener(StartGame);
            quitButton.onClick.AddListener(QuitGame);

            RectTransform fadeRect = NewRect("Fade", rootCanvas.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image fadeImage = fadeRect.gameObject.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeGroup = fadeRect.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void EnsureTrafficLightWorldLabelsVisible()
        {
            TrafficLightDebugSettings.ShowTrafficLightDebugInfo = true;
            TrafficLightDebugSettings.ShowWorldLabels = true;
            TrafficLightDebugSettings.ShowExtraInfo = true;

            TrafficLightController[] lights = Object.FindObjectsByType<TrafficLightController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                TrafficLightController light = lights[i];
                if (light == null) continue;
                if (light.GetComponent<TrafficLightWorldLabel>() == null)
                {
                    light.gameObject.AddComponent<TrafficLightWorldLabel>();
                }
            }
        }

        private Button MakeButton(RectTransform parent, string label, Vector2 pos)
        {
            RectTransform rect = NewRect(label.Replace(" ", "") + "Button", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(300f, 66f));
            Image bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.07f, 0.64f);

            Outline glow = rect.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(accent.r, accent.g, accent.b, 0.62f);
            glow.effectDistance = new Vector2(2.2f, 2.2f);

            Button btn = rect.gameObject.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bg.color;
            cb.highlightedColor = new Color(0.16f, 0.14f, 0.11f, 0.9f);
            cb.pressedColor = new Color(0.2f, 0.17f, 0.13f, 0.95f);
            cb.selectedColor = cb.highlightedColor;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.12f;
            btn.colors = cb;

            TextMeshProUGUI txt = MakeText("Label", rect, label, 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.75f, 1f));
            txt.characterSpacing = 1.6f;
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 18f;
            txt.fontSizeMax = 34f;

            FuturisticMenuButtonFx fx = rect.gameObject.AddComponent<FuturisticMenuButtonFx>();
            fx.Configure(rect, bg, txt, accent);
            return btn;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return rect;
        }

        private static TextMeshProUGUI MakeText(string name, RectTransform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = value;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            return tmp;
        }

        private static RawImage MakeRawImage(string name, RectTransform parent, Texture tex, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RawImage image = go.GetComponent<RawImage>();
            image.texture = tex;
            image.color = color;
            return image;
        }

        private static Image MakeImage(string name, RectTransform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Texture2D MakeVerticalGradient(Color bottom, Color top)
        {
            Texture2D tex = new Texture2D(2, 128, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < tex.height; y++)
            {
                float t = y / (tex.height - 1f);
                Color c = Color.Lerp(bottom, top, t);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeGridTexture(Color line, Color bg)
        {
            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool grid = x % 24 == 0 || y % 24 == 0 || x % 48 == 0 || y % 48 == 0;
                    tex.SetPixel(x, y, grid ? line : bg);
                }
            }
            tex.Apply();
            return tex;
        }
    }

    [DisallowMultipleComponent]
    public class FuturisticMenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform rect;
        private Image bg;
        private TextMeshProUGUI txt;
        private Color accent;
        private float hover;
        private bool pressed;

        public void Configure(RectTransform targetRect, Image targetBg, TextMeshProUGUI targetText, Color accentColor)
        {
            rect = targetRect;
            bg = targetBg;
            txt = targetText;
            accent = accentColor;
        }

        private void Update()
        {
            if (rect == null || bg == null || txt == null) return;

            float target = pressed ? 0.2f : hover;
            float s = Mathf.Lerp(rect.localScale.x, 1f + target * 0.035f, Time.unscaledDeltaTime * 13f);
            rect.localScale = new Vector3(s, s, 1f);

            Color baseColor = Color.Lerp(new Color(0.07f, 0.07f, 0.07f, 0.64f), new Color(0.19f, 0.16f, 0.12f, 0.9f), target);
            bg.color = baseColor;
            txt.color = Color.Lerp(new Color(1f, 0.92f, 0.75f, 1f), accent, target * 0.8f);
        }

        public void OnPointerEnter(PointerEventData eventData) => hover = 1f;
        public void OnPointerExit(PointerEventData eventData) => hover = 0f;
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
    }
}
