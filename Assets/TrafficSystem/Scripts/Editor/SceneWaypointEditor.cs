#if UNITY_EDITOR
using MyTrafficSystem.Lanes;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    [InitializeOnLoad]
    public static class SceneWaypointEditor
    {
        static SceneWaypointEditor()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView view)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F1)
            {
                TrafficDebugSettings.ToggleTrafficDebug();
                SceneView.RepaintAll();
                e.Use();
                return;
            }

            if (!TrafficDebugSettings.ShowTrafficDebug)
            {
                return;
            }

            if (e.alt || e.button == 1 || e.button == 2 || e.type == EventType.ScrollWheel)
            {
                return;
            }

            Lane[] lanes = Object.FindObjectsByType<Lane>(FindObjectsSortMode.None);
            for (int l = 0; l < lanes.Length; l++)
            {
                Lane lane = lanes[l];
                if (lane == null)
                {
                    continue;
                }

                DrawLaneWaypointHandles(lane);
            }
        }

        private static void DrawLaneWaypointHandles(Lane lane)
        {
            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                Waypoint wp = lane.Waypoints[i];
                if (wp == null)
                {
                    continue;
                }

                Handles.color = i == 0 ? Color.green : (i == lane.Waypoints.Count - 1 ? Color.red : Color.white);
                float size = HandleUtility.GetHandleSize(wp.transform.position) * 0.12f;

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(wp.transform.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(wp.transform, "Move Waypoint");
                    wp.transform.position = newPos;
                    EditorUtility.SetDirty(wp);
                    EditorUtility.SetDirty(lane);
                }
            }
        }
    }
}
#endif
