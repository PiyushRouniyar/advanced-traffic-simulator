using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyTrafficSystem.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class TrafficControlCenterTheme : MonoBehaviour
    {
        [SerializeField] private Color panelColor = new Color(0.04f, 0.08f, 0.12f, 0.86f);
        [SerializeField] private Color cyanText = new Color(0.5f, 0.95f, 1f, 1f);
        [SerializeField] private Color accentGreen = new Color(0.42f, 1f, 0.62f, 1f);
        [SerializeField] private Color accentRed = new Color(1f, 0.35f, 0.35f, 1f);

        private void Awake()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null) continue;
                string n = images[i].name.ToLowerInvariant();
                if (n.Contains("panel") || n.Contains("background") || n.Contains("border"))
                {
                    images[i].color = panelColor;
                }
            }

            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;
                string n = texts[i].name.ToLowerInvariant();
                if (n.Contains("congestion") || n.Contains("warning")) texts[i].color = accentRed;
                else if (n.Contains("flow") || n.Contains("status") || n.Contains("pedestrian")) texts[i].color = accentGreen;
                else texts[i].color = cyanText;
            }

            Slider[] sliders = GetComponentsInChildren<Slider>(true);
            for (int i = 0; i < sliders.Length; i++)
            {
                if (sliders[i] == null || sliders[i].fillRect == null) continue;
                Image fill = sliders[i].fillRect.GetComponent<Image>();
                if (fill == null) continue;
                string n = sliders[i].name.ToLowerInvariant();
                fill.color = n.Contains("congestion") ? accentRed : new Color(1f, 0.76f, 0.2f, 1f);
            }
        }
    }
}
