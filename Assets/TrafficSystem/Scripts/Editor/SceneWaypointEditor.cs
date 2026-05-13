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
            if (!Application.isPlaying && e.type == EventType.KeyDown && e.keyCode == KeyCode.F1)
            {
                TrafficDebugSettings.ToggleClutterOnly();
                SceneView.RepaintAll();
                e.Use();
                return;
            }

            if (!Application.isPlaying && e.type == EventType.KeyDown && e.keyCode == KeyCode.F2)
            {
                TrafficDebugSettings.ShowLaneLabels = !TrafficDebugSettings.ShowLaneLabels;
                SceneView.RepaintAll();
                e.Use();
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

                DrawLaneLabel(lane);
                DrawLaneWaypointHandles(lane);
            }
        }

        private static void DrawLaneWaypointHandles(Lane lane)
        {
            if (!TrafficDebugSettings.ShowWaypointHandles)
            {
                return;
            }

            Event e = Event.current;
            if (e.alt || e.button == 1 || e.button == 2 || e.type == EventType.ScrollWheel)
            {
                return;
            }

            bool selectedLane = Selection.activeGameObject == lane.gameObject;
            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                Waypoint wp = lane.Waypoints[i];
                if (wp == null)
                {
                    continue;
                }

                bool selectedWaypoint = Selection.activeGameObject == wp.gameObject;
                Handles.color = selectedWaypoint ? Color.yellow : (i == 0 ? Color.green : (i == lane.Waypoints.Count - 1 ? Color.red : Color.white));
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

                if (selectedLane)
                {
                    Handles.Label(wp.transform.position + Vector3.up * 0.3f, $"WP {i}");
                }
            }

            if (selectedLane)
            {
                DrawInsertWaypointButtons(lane);
            }
        }

        private static void DrawInsertWaypointButtons(Lane lane)
        {
            for (int i = 0; i < lane.Waypoints.Count - 1; i++)
            {
                Waypoint a = lane.Waypoints[i];
                Waypoint b = lane.Waypoints[i + 1];
                if (a == null || b == null)
                {
                    continue;
                }

                Vector3 mid = Vector3.Lerp(a.transform.position, b.transform.position, 0.5f);
                float size = HandleUtility.GetHandleSize(mid) * 0.08f;
                Handles.color = Color.yellow;
                if (Handles.Button(mid, Quaternion.identity, size, size, Handles.SphereHandleCap))
                {
                    InsertWaypoint(lane, i + 1, mid);
                    return;
                }
            }
        }

        private static void InsertWaypoint(Lane lane, int index, Vector3 position)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Insert Waypoint");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.SetSiblingIndex(index);
            wpObj.transform.position = position;

            Waypoint wp = Undo.AddComponent<Waypoint>(wpObj);
            Undo.RecordObject(lane, "Insert Waypoint");
            lane.InsertWaypointAt(index, wp);
            EditorUtility.SetDirty(lane);
            Selection.activeGameObject = wpObj;
        }

        private static void DrawLaneLabel(Lane lane)
        {
            if (!TrafficDebugSettings.ShowLaneLabels)
            {
                return;
            }

            Vector3 anchor = lane.transform.position;
            int count = 0;
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                Waypoint wp = lane.Waypoints[i];
                if (wp == null)
                {
                    continue;
                }

                sum += wp.transform.position;
                count++;
            }

            if (count > 0)
            {
                anchor = sum / count;
            }

            anchor += Vector3.up * 0.85f;
            bool selectedLane = Selection.activeGameObject == lane.gameObject;
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = selectedLane ? Color.white : new Color(0.86f, 0.9f, 0.95f, 0.75f);

            string text = lane.LaneName;
            if (selectedLane)
            {
                if (TrafficDebugSettings.ShowLaneIds)
                {
                    text += $"  [ID:{lane.GetInstanceID()}]";
                }
                if (TrafficDebugSettings.ShowWaypointLabels)
                {
                    text += $"  [WP:{lane.Waypoints.Count}]";
                }
            }

            Handles.Label(anchor, text, style);
        }
    }
}
#endif
