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
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F3)
            {
                TrafficLightDebugSettings.ShowTrafficLightDebugInfo = !TrafficLightDebugSettings.ShowTrafficLightDebugInfo;
                SceneView.RepaintAll();
                e.Use();
                return;
            }

            if (!TrafficLightDebugSettings.ShowTrafficLightDebugInfo)
            {
                return;
            }

            TrafficLightGroup[] groups = Object.FindObjectsByType<TrafficLightGroup>(FindObjectsSortMode.None);
            for (int i = 0; i < groups.Length; i++)
            {
                TrafficLightGroup group = groups[i];
                if (group == null)
                {
                    continue;
                }

                DrawGroupLabel(group);
            }
        }

        private static void DrawGroupLabel(TrafficLightGroup group)
        {
            Vector3 pos = group.transform.position + Vector3.up * 3f;
            bool selected = Selection.activeGameObject == group.gameObject;

            GUIStyle keyStyle = new GUIStyle(EditorStyles.boldLabel);
            keyStyle.normal.textColor = selected ? Color.white : new Color(0.85f, 0.9f, 1f, 0.9f);

            GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel);
            stateStyle.normal.textColor = group.DebugState == TrafficLightState.Green ? Color.green :
                                          group.DebugState == TrafficLightState.Red ? Color.red :
                                          new Color(1f, 0.85f, 0.15f, 1f);

            Handles.Label(pos, $"Key: {group.ActivationKey}", keyStyle);
            Handles.Label(pos + Vector3.down * 0.25f, $"State: {group.DebugState}", stateStyle);
        }
    }
}
#endif
