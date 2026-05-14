using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    public class TrafficLightUIController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Image dim;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI lightName;
        [SerializeField] private TextMeshProUGUI currentState;
        [SerializeField] private TextMeshProUGUI trafficStatus;
        [SerializeField] private TextMeshProUGUI currentMode;
        [SerializeField] private Button greenButton;
        [SerializeField] private Button redButton;
        [SerializeField] private Button closeButton;

        [SerializeField] private float animSpeed = 12f;

        public UnityEvent onTurnGreen = new UnityEvent();
        public UnityEvent onTurnRed = new UnityEvent();
        public UnityEvent onClose = new UnityEvent();

        public bool IsOpen { get; private set; }

        private TrafficLightInteractable target;

        public void Initialize(CanvasGroup cg, RectTransform root, Image dimBg, TextMeshProUGUI t, TextMeshProUGUI name,
            TextMeshProUGUI state, TextMeshProUGUI status, TextMeshProUGUI mode, Button green, Button red, Button close)
        {
            canvasGroup = cg;
            panelRoot = root;
            dim = dimBg;
            title = t;
            lightName = name;
            currentState = state;
            trafficStatus = status;
            currentMode = mode;
            greenButton = green;
            redButton = red;
            closeButton = close;

            HookButtons();
            SetImmediate(false);
        }

        private void HookButtons()
        {
            if (greenButton != null) greenButton.onClick.AddListener(() => onTurnGreen.Invoke());
            if (redButton != null) redButton.onClick.AddListener(() => onTurnRed.Invoke());
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            float a = IsOpen ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, a, animSpeed * Time.unscaledDeltaTime);
            canvasGroup.blocksRaycasts = IsOpen;
            canvasGroup.interactable = IsOpen;

            float s = IsOpen ? 1f : 0.92f;
            panelRoot.localScale = Vector3.Lerp(panelRoot.localScale, Vector3.one * s, 1f - Mathf.Exp(-animSpeed * Time.unscaledDeltaTime));

            if (dim != null)
            {
                Color c = dim.color;
                c.a = Mathf.Lerp(c.a, IsOpen ? 0.52f : 0f, 1f - Mathf.Exp(-animSpeed * Time.unscaledDeltaTime));
                dim.color = c;
            }

            if (IsOpen && target != null)
            {
                Refresh(target);
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open(TrafficLightInteractable interactable)
        {
            target = interactable;
            Refresh(target);
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
            onClose.Invoke();
        }

        private void Refresh(TrafficLightInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            title.text = "Traffic Controller";
            lightName.text = $"Light: {interactable.DisplayName}";
            currentState.text = $"Current State: {interactable.CurrentStateText}";
            trafficStatus.text = $"Nearby Traffic: {interactable.EstimateWaitingCars()} vehicles";
            currentMode.text = $"Current Mode: {interactable.CurrentModeText}";

            bool isGreen = interactable.Light != null && interactable.Light.CurrentState == MyTrafficSystem.TrafficLights.TrafficLightState.Green;
            if (greenButton != null) greenButton.interactable = !isGreen;
            if (redButton != null) redButton.interactable = isGreen;
        }

        private void SetImmediate(bool open)
        {
            IsOpen = open;
            canvasGroup.alpha = open ? 1f : 0f;
            canvasGroup.blocksRaycasts = open;
            canvasGroup.interactable = open;
            panelRoot.localScale = Vector3.one * (open ? 1f : 0.92f);
        }
    }
}
