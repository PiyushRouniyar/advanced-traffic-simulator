using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class TrafficGameHUD : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI flowText;
        [SerializeField] private TextMeshProUGUI pedestrianText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI cameraText;
        [SerializeField] private TextMeshProUGUI gameStateText;
        [SerializeField] private TextMeshProUGUI introText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Meters")]
        [SerializeField] private Slider congestionSlider;
        [SerializeField] private Slider pressureSlider;

        [Header("Panels")]
        [SerializeField] private CanvasGroup introPanel;
        [SerializeField] private CanvasGroup resultPanel;

        public void SetLevelName(string name)
        {
            if (levelNameText != null) levelNameText.text = name;
        }

        public void SetTimer(float remainingSeconds)
        {
            if (timerText == null) return;
            int mins = Mathf.FloorToInt(remainingSeconds / 60f);
            int secs = Mathf.FloorToInt(remainingSeconds % 60f);
            timerText.text = $"Time {mins:00}:{secs:00}";
        }

        public void SetCongestion(float normalized, int stalled, int active)
        {
            if (congestionSlider != null) congestionSlider.value = Mathf.Clamp01(normalized);
            if (congestionText != null) congestionText.text = $"Congestion {(normalized * 100f):0}% ({stalled}/{active} stalled)";
        }

        public void SetFlow(float flow01)
        {
            if (flowText != null) flowText.text = $"Flow {(flow01 * 100f):0}%";
        }

        public void SetPedestrianStatus(int waiting)
        {
            if (pedestrianText != null) pedestrianText.text = $"Pedestrians waiting: {waiting}";
        }

        public void SetScore(int score)
        {
            if (scoreText != null) scoreText.text = $"Score {score}";
        }

        public void SetCamera(string label)
        {
            if (cameraText != null) cameraText.text = $"Camera {label}";
        }

        public void SetGameState(string label)
        {
            if (gameStateText != null) gameStateText.text = label;
        }

        public void SetPressure(float pressure01)
        {
            if (pressureSlider != null) pressureSlider.value = Mathf.Clamp01(pressure01);
        }

        public void ShowIntro(string text)
        {
            if (introText != null) introText.text = text;
            SetPanel(introPanel, true);
        }

        public void HideIntro() => SetPanel(introPanel, false);

        public void ShowResult(string text)
        {
            if (resultText != null) resultText.text = text;
            SetPanel(resultPanel, true);
        }

        public void HideResult() => SetPanel(resultPanel, false);

        private static void SetPanel(CanvasGroup panel, bool visible)
        {
            if (panel == null) return;
            panel.alpha = visible ? 1f : 0f;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
        }
    }
}
