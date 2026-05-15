using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class CCTVCameraOverlayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CCTVCameraSystem cctvSystem;
        [SerializeField] private TextMeshProUGUI cameraNameText;
        [SerializeField] private TextMeshProUGUI intersectionNameText;
        [SerializeField] private TextMeshProUGUI clockText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI trafficGroupText;
        [SerializeField] private TextMeshProUGUI cameraNumberText;
        [SerializeField] private Image recordingDot;
        [SerializeField] private Image scanlineOverlay;
        [SerializeField] private Image noiseOverlay;
        [SerializeField] private Image monitorBorder;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Button setAllGreenButton;
        [SerializeField] private Button setAllRedButton;
        [SerializeField] private MasterTrafficLightController masterTrafficLightController;

        [Header("Style")]
        [SerializeField] private Color goodColor = new Color(0.35f, 1f, 0.75f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.4f, 0.35f, 1f);
        [SerializeField] private bool blinkRecordingDot = true;
        [SerializeField] private bool useSubtleFlicker = true;
        [SerializeField] private bool animateScanlines = true;
        [SerializeField] private float scanlineSpeed = 22f;
        [SerializeField] private float noisePulseSpeed = 3.5f;
        [SerializeField] private string overlayPrefix = "REC ●";

        private void Awake()
        {
            if (cctvSystem == null)
            {
                cctvSystem = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            }
            if (masterTrafficLightController == null)
            {
                masterTrafficLightController = FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include);
            }

            if (cctvSystem != null)
            {
                cctvSystem.CameraChanged += OnCameraChanged;
                RefreshFromSystem();
            }

            WireLightButtons();
        }

        private void OnDestroy()
        {
            if (cctvSystem != null)
            {
                cctvSystem.CameraChanged -= OnCameraChanged;
            }
        }

        private void Update()
        {
            if (clockText != null)
            {
                clockText.text = System.DateTime.Now.ToString("HH:mm:ss");
            }

            if (recordingDot != null && blinkRecordingDot)
            {
                float pulse = 0.55f + Mathf.PingPong(Time.unscaledTime * 0.8f, 0.45f);
                Color c = recordingDot.color;
                c.a = pulse;
                recordingDot.color = c;
            }

            if (statusText != null && cctvSystem != null)
            {
                bool valid = cctvSystem.CameraCount > 0 && cctvSystem.ActiveCameraIndex >= 0;
                statusText.text = valid ? $"{overlayPrefix} ONLINE" : "NO FEED";
                statusText.color = valid ? goodColor : warningColor;
            }

            if (cameraNumberText != null && cctvSystem != null)
            {
                cameraNumberText.text = $"CAM {Mathf.Max(1, cctvSystem.ActiveCameraIndex + 1):00}";
            }

            if (trafficGroupText != null && cctvSystem != null)
            {
                trafficGroupText.text = cctvSystem.ActiveTrafficGroupName;
            }

            if (scanlineOverlay != null && animateScanlines)
            {
                RectTransform rt = scanlineOverlay.rectTransform;
                Vector2 p = rt.anchoredPosition;
                p.y = Mathf.Repeat(Time.unscaledTime * scanlineSpeed, 64f) - 32f;
                rt.anchoredPosition = p;
            }

            if (noiseOverlay != null)
            {
                Color c = noiseOverlay.color;
                c.a = 0.04f + Mathf.PingPong(Time.unscaledTime * noisePulseSpeed, 0.05f);
                noiseOverlay.color = c;
            }

            if (rootGroup != null && useSubtleFlicker)
            {
                rootGroup.alpha = 0.95f + Mathf.PingPong(Time.unscaledTime * 0.5f, 0.05f);
            }
        }

        public void SetVisible(bool visible)
        {
            if (rootGroup == null) return;
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }

        private void OnCameraChanged(CCTVCameraPoint point, int index)
        {
            RefreshFromSystem();
        }

        private void RefreshFromSystem()
        {
            if (cctvSystem == null) return;

            if (cameraNameText != null)
            {
                cameraNameText.text = $"{cctvSystem.ActiveCameraObjectName}  [{cctvSystem.ActiveCameraIndex + 1}/{Mathf.Max(1, cctvSystem.CameraCount)}]";
            }

            if (intersectionNameText != null)
            {
                intersectionNameText.text = cctvSystem.ActiveIntersectionName;
            }
        }

        private void WireLightButtons()
        {
            if (setAllGreenButton != null)
            {
                setAllGreenButton.onClick.RemoveListener(SetAllGreenFromUI);
                setAllGreenButton.onClick.AddListener(SetAllGreenFromUI);
            }

            if (setAllRedButton != null)
            {
                setAllRedButton.onClick.RemoveListener(SetAllRedFromUI);
                setAllRedButton.onClick.AddListener(SetAllRedFromUI);
            }
        }

        public void SetAllGreenFromUI()
        {
            if (masterTrafficLightController == null)
            {
                masterTrafficLightController = FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include);
            }

            masterTrafficLightController?.SetAllGreen();
        }

        public void SetAllRedFromUI()
        {
            if (masterTrafficLightController == null)
            {
                masterTrafficLightController = FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include);
            }

            masterTrafficLightController?.SetAllRed();
        }
    }
}
