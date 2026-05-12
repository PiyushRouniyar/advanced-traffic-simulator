#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using MyTrafficSystem.Lanes;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    [InitializeOnLoad]
    public static class CitizenSceneWaypointEditor
    {
        static CitizenSceneWaypointEditor()
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

            CitizenLane[] lanes = Object.FindObjectsByType<CitizenLane>(FindObjectsSortMode.None);
            for (int i = 0; i < lanes.Length; i++)
            {
                CitizenLane lane = lanes[i];
                if (lane == null)
                {
                    continue;
                }

                DrawWaypointHandles(lane);
            }
        }

        private static void DrawWaypointHandles(CitizenLane lane)
        {
            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                PedestrianWaypoint wp = lane.Waypoints[i];
                if (wp == null)
                {
                    continue;
                }

                Handles.color = i == 0 ? Color.green : (i == lane.Waypoints.Count - 1 ? Color.red : Color.white);
                float size = HandleUtility.GetHandleSize(wp.transform.position) * 0.12f;

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.FreeMoveHandle(wp.transform.position, size, Vector3.zero, Handles.DotHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(wp.transform, "Move Citizen Waypoint");
                    wp.transform.position = newPos;
                    EditorUtility.SetDirty(wp);
                    EditorUtility.SetDirty(lane);
                }
            }
        }
    }
}
#endif
