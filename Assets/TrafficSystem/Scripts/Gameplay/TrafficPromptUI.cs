using TMPro;
using UnityEngine;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    public class TrafficPromptUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobFrequency = 2.5f;

        private Camera targetCamera;
        private RectTransform rect;
        private float baseY;
        private bool visible;

        public void Initialize(CanvasGroup group, TextMeshProUGUI textLabel)
        {
            canvasGroup = group;
            label = textLabel;
        }

        private void Awake()
        {
            rect = transform as RectTransform;
            baseY = rect.anchoredPosition.y;
            SetVisibleImmediate(false);
        }

        private void Update()
        {
            if (targetCamera != null)
            {
                transform.forward = targetCamera.transform.forward;
            }

            if (rect != null)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.y = baseY + Mathf.Sin(Time.unscaledTime * bobFrequency) * bobAmplitude * 20f;
                rect.anchoredPosition = pos;
            }

            float targetAlpha = visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            canvasGroup.blocksRaycasts = visible;
        }

        public void BindCamera(Camera cam)
        {
            targetCamera = cam;
        }

        public void SetText(string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        public void SetVisible(bool shouldShow)
        {
            visible = shouldShow;
        }

        public void SetVisibleImmediate(bool shouldShow)
        {
            visible = shouldShow;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = shouldShow ? 1f : 0f;
                canvasGroup.blocksRaycasts = shouldShow;
            }
        }
    }
}
