using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class TrafficUIButtonFeedback : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        [Header("Animation")]
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressedScale = 0.96f;
        [SerializeField] private float speed = 18f;
        [SerializeField] private float hoverBrightness = 1.12f;
        [SerializeField] private float disabledBrightness = 0.45f;

        [Header("Pulse")]
        [SerializeField] private bool pulseEnabled;
        [SerializeField] private float pulseAmplitude = 0.05f;
        [SerializeField] private float pulseSpeed = 2.4f;

        [Header("Sound Hooks")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;

        private Vector3 defaultScale;
        private Vector3 targetScale;
        private Image image;
        private Color baseColor;
        private UnityEngine.UI.Button button;

        public void SetPulseEnabled(bool enabled)
        {
            pulseEnabled = enabled;
        }

        private void Awake()
        {
            image = GetComponent<Image>();
            button = GetComponent<UnityEngine.UI.Button>();
            baseColor = image.color;
            defaultScale = transform.localScale;
            targetScale = defaultScale;
        }

        private void Update()
        {
            float pulse = 1f;
            if (pulseEnabled && button != null && button.interactable)
            {
                pulse += Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            }

            Vector3 pulseScale = targetScale * pulse;
            transform.localScale = Vector3.Lerp(transform.localScale, pulseScale, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));

            Color desiredColor = baseColor;
            if (button != null && !button.interactable)
            {
                desiredColor = baseColor * disabledBrightness;
            }

            image.color = Color.Lerp(image.color, desiredColor, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (button != null && !button.interactable)
            {
                return;
            }

            targetScale = defaultScale * hoverScale;
            image.color = baseColor * hoverBrightness;
            if (audioSource != null && hoverClip != null)
            {
                audioSource.PlayOneShot(hoverClip);
            }
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            targetScale = defaultScale;
            image.color = baseColor;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (button != null && !button.interactable)
            {
                return;
            }

            targetScale = defaultScale * pressedScale;
            if (audioSource != null && clickClip != null)
            {
                audioSource.PlayOneShot(clickClip);
            }
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (button != null && !button.interactable)
            {
                return;
            }

            targetScale = defaultScale * hoverScale;
        }
    }
}
