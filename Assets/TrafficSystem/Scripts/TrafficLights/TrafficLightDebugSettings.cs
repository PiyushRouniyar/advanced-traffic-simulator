using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    public static class TrafficLightDebugSettings
    {
        public static bool ShowTrafficLightDebugInfo = true;
        public static bool ShowSceneViewLabels = true;
        public static bool ShowWorldLabels = true;
        public static bool ShowExtraInfo = true;

        public static KeyCode ToggleKey = KeyCode.F3;

        public static float LabelHeight = 3.2f;
        public static float LabelUpdateInterval = 0.15f;
    }
}
