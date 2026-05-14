#if UNITY_EDITOR
using MyTrafficSystem.TrafficLights;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    [InitializeOnLoad]
    public static class TrafficLightSceneDebugEditor
    {
        static TrafficLightSceneDebugEditor()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == TrafficLightDebugSettings.ToggleKey)
            {
                TrafficLightDebugSettings.ShowTrafficLightDebugInfo = !TrafficLightDebugSettings.ShowTrafficLightDebugInfo;
                SceneView.RepaintAll();
                e.Use();
                return;
            }

            if (!TrafficLightDebugSettings.ShowTrafficLightDebugInfo || !TrafficLightDebugSettings.ShowSceneViewLabels)
            {
                return;
            }

            MyTrafficSystem.TrafficLights.TrafficLightController[] lights = Object.FindObjectsByType<MyTrafficSystem.TrafficLights.TrafficLightController>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                MyTrafficSystem.TrafficLights.TrafficLightController light = lights[i];
                if (light == null)
                {
                    continue;
                }

                DrawLightLabel(light);
            }
        }

        private static void DrawLightLabel(MyTrafficSystem.TrafficLights.TrafficLightController light)
        {
            Vector3 pos = light.transform.position + Vector3.up * TrafficLightDebugSettings.LabelHeight;
            bool selected = Selection.activeGameObject == light.gameObject;

            GUIStyle keyStyle = new GUIStyle(EditorStyles.boldLabel);
            keyStyle.normal.textColor = selected ? Color.white : new Color(0.85f, 0.9f, 1f, 0.9f);

            GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel);
            stateStyle.normal.textColor = light.CurrentState == TrafficLightState.Green ? Color.green :
                                          light.CurrentState == TrafficLightState.Red ? Color.red :
                                          new Color(1f, 0.85f, 0.15f, 1f);

            string groupName = "No Group";
            TrafficLightGroup group = light.GetComponentInParent<TrafficLightGroup>();
            if (group != null)
            {
                groupName = group.GroupName;
            }

            Handles.Label(pos, $"[{light.CurrentState.ToString().ToUpperInvariant()}]", stateStyle);
            Handles.Label(pos + Vector3.down * 0.23f, $"Key: {light.KeyboardToggleKey}", keyStyle);

            if (TrafficLightDebugSettings.ShowExtraInfo)
            {
                Handles.Label(pos + Vector3.down * 0.46f, $"Auto: {(light.AutoCycleEnabled ? "ON" : "OFF")}  Timer: {light.RemainingTimer:0.0}s", keyStyle);
                Handles.Label(pos + Vector3.down * 0.69f, $"{light.gameObject.name} | {groupName}", keyStyle);
            }
        }
    }
}
#endif
