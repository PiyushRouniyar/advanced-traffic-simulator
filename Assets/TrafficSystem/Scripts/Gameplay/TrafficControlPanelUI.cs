using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    public class TrafficControlPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Image dimBackground;

        [Header("Info Labels")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI intersectionLabel;
        [SerializeField] private TextMeshProUGUI stateLabel;
        [SerializeField] private TextMeshProUGUI directionLabel;
        [SerializeField] private TextMeshProUGUI waitingCountLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI congestionLabel;
        [SerializeField] private Image stateIndicator;

        [Header("Buttons")]
        [SerializeField] private Button switchPhaseButton;
        [SerializeField] private Button emergencyButton;
        [SerializeField] private Button autoModeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI autoModeButtonLabel;

        [Header("Animation")]
        [SerializeField] private float openSpeed = 10f;
        [SerializeField] private float panelScaleOpen = 1f;
        [SerializeField] private float panelScaleClosed = 0.92f;

        public UnityEvent onSwitchPhase = new UnityEvent();
        public UnityEvent onEmergency = new UnityEvent();
        public UnityEvent<bool> onAutoMode = new UnityEvent<bool>();
        public UnityEvent onClosed = new UnityEvent();

        public bool IsOpen { get; private set; }

        private TrafficIntersectionInteractable current;
        private bool autoMode;
        private bool initialized;

        public void Initialize(
            CanvasGroup canvasGroup,
            RectTransform panel,
            Image dim,
            TextMeshProUGUI title,
            TextMeshProUGUI intersection,
            TextMeshProUGUI state,
            TextMeshProUGUI direction,
            TextMeshProUGUI waiting,
            TextMeshProUGUI timer,
            TextMeshProUGUI congestion,
            Image indicator,
            Button switchPhase,
            Button emergency,
            Button autoModeBtn,
            Button close,
            TextMeshProUGUI autoLabel)
        {
            rootGroup = canvasGroup;
            panelRoot = panel;
            dimBackground = dim;
            titleLabel = title;
            intersectionLabel = intersection;
            stateLabel = state;
            directionLabel = direction;
            waitingCountLabel = waiting;
            timerLabel = timer;
            congestionLabel = congestion;
            stateIndicator = indicator;
            switchPhaseButton = switchPhase;
            emergencyButton = emergency;
            autoModeButton = autoModeBtn;
            closeButton = close;
            autoModeButtonLabel = autoLabel;

            TrySetup();
        }

        private void Awake()
        {
            EnsureEvents();
            TrySetup();
        }

        private void OnEnable()
        {
            EnsureEvents();
        }

        private void EnsureEvents()
        {
            onSwitchPhase ??= new UnityEvent();
            onEmergency ??= new UnityEvent();
            onAutoMode ??= new UnityEvent<bool>();
            onClosed ??= new UnityEvent();
        }

        private void TrySetup()
        {
            if (initialized)
            {
                return;
            }

            if (rootGroup == null || panelRoot == null)
            {
                return;
            }

            if (switchPhaseButton != null) switchPhaseButton.onClick.AddListener(() => onSwitchPhase?.Invoke());
            if (emergencyButton != null) emergencyButton.onClick.AddListener(() => onEmergency?.Invoke());
            if (autoModeButton != null) autoModeButton.onClick.AddListener(ToggleAutoMode);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            initialized = true;
            SetOpenImmediate(false);
        }

        private void Update()
        {
            if (!initialized)
            {
                TrySetup();
                return;
            }

            float targetAlpha = IsOpen ? 1f : 0f;
            rootGroup.alpha = Mathf.MoveTowards(rootGroup.alpha, targetAlpha, openSpeed * Time.unscaledDeltaTime);
            rootGroup.interactable = IsOpen;
            rootGroup.blocksRaycasts = IsOpen;

            float targetScale = IsOpen ? panelScaleOpen : panelScaleClosed;
            panelRoot.localScale = Vector3.Lerp(panelRoot.localScale, Vector3.one * targetScale, 1f - Mathf.Exp(-openSpeed * Time.unscaledDeltaTime));

            if (dimBackground != null)
            {
                Color c = dimBackground.color;
                c.a = Mathf.Lerp(c.a, IsOpen ? 0.55f : 0f, 1f - Mathf.Exp(-openSpeed * Time.unscaledDeltaTime));
                dimBackground.color = c;
            }

            if (IsOpen && current != null)
            {
                RefreshInfo(current);
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        public void Open(TrafficIntersectionInteractable interactable)
        {
            if (!initialized || interactable == null)
            {
                return;
            }

            current = interactable;
            autoMode = current.AutoModeEnabled;
            UpdateAutoModeLabel();
            RefreshInfo(current);
            IsOpen = true;
            EventSystem.current?.SetSelectedGameObject(switchPhaseButton != null ? switchPhaseButton.gameObject : null);
        }

        public void Close()
        {
            IsOpen = false;
            onClosed?.Invoke();
        }

        private void ToggleAutoMode()
        {
            autoMode = !autoMode;
            UpdateAutoModeLabel();
            onAutoMode?.Invoke(autoMode);
        }

        private void UpdateAutoModeLabel()
        {
            if (autoModeButtonLabel != null)
            {
                autoModeButtonLabel.text = autoMode ? "Auto Mode: ON" : "Auto Mode: OFF";
            }
        }

        private void RefreshInfo(TrafficIntersectionInteractable interactable)
        {
            if (titleLabel != null) titleLabel.text = "Traffic Command";
            if (intersectionLabel != null) intersectionLabel.text = interactable.IntersectionName;

            int waiting = interactable.EstimateWaitingCars();
            string direction = interactable.GetActiveDirectionLabel();
            float timer = interactable.GetCurrentTimer();
            int greenIndex = interactable.GetCurrentGreenIndex();

            string state = "Red";
            var groups = interactable.Groups;
            if (groups != null && greenIndex >= 0 && greenIndex < groups.Length && groups[greenIndex] != null)
            {
                state = groups[greenIndex].DebugState.ToString();
            }

            if (stateLabel != null) stateLabel.text = $"Phase: {state}";
            if (directionLabel != null) directionLabel.text = $"Direction: {direction}";
            if (timerLabel != null) timerLabel.text = $"Timer: {timer:0.0}s";
            if (waitingCountLabel != null) waitingCountLabel.text = $"Waiting Cars: {waiting}";

            if (congestionLabel != null)
            {
                if (waiting >= 12)
                {
                    congestionLabel.text = "Congestion: Heavy";
                    congestionLabel.color = new Color(1f, 0.45f, 0.37f, 1f);
                }
                else if (waiting >= 6)
                {
                    congestionLabel.text = "Congestion: Moderate";
                    congestionLabel.color = new Color(1f, 0.77f, 0.34f, 1f);
                }
                else
                {
                    congestionLabel.text = "Congestion: Light";
                    congestionLabel.color = new Color(0.48f, 0.95f, 0.72f, 1f);
                }
            }

            if (stateIndicator != null)
            {
                stateIndicator.color = state == "Green" ? new Color(0.3f, 0.95f, 0.63f, 1f) :
                                       state == "Yellow" ? new Color(0.98f, 0.78f, 0.27f, 1f) :
                                       new Color(1f, 0.4f, 0.4f, 1f);
            }
        }

        private void SetOpenImmediate(bool open)
        {
            IsOpen = open;
            rootGroup.alpha = open ? 1f : 0f;
            rootGroup.blocksRaycasts = open;
            rootGroup.interactable = open;
            panelRoot.localScale = Vector3.one * (open ? panelScaleOpen : panelScaleClosed);
        }
    }
}
