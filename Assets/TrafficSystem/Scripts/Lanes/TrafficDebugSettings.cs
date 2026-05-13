using UnityEngine;

namespace MyTrafficSystem.Lanes
{
    public static class TrafficDebugSettings
    {
        public static bool ShowTrafficDebug = true;
        public static bool ShowLanePaths = true;
        public static bool ShowDirectionArrows = true;
        public static bool ShowWaypointHandles = true;
        public static bool ShowLaneLabels = true;
        public static bool ShowWaypointLabels = true;
        public static bool ShowLaneIds = true;
        public static bool ShowConnectionArrows = true;
        public static bool DrawLaneDirections = true;

        public static void ToggleTrafficDebug()
        {
            ShowTrafficDebug = !ShowTrafficDebug;
        }

        public static void ToggleClutterOnly()
        {
            bool hideClutter = ShowDirectionArrows || ShowWaypointHandles;
            ShowDirectionArrows = !hideClutter;
            ShowWaypointHandles = !hideClutter;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeRuntimeToggle()
        {
            GameObject go = new GameObject("TrafficDebugToggleRuntime");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<TrafficDebugToggleRuntime>();
        }
    }

    [DisallowMultipleComponent]
    public class TrafficDebugToggleRuntime : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TrafficDebugSettings.ToggleClutterOnly();
            }
        }
    }
}
