#if UNITY_EDITOR
using System.Collections.Generic;
using MyTrafficSystem.Lanes;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    public class TrafficLightManagerWindow : EditorWindow
    {
        private GameObject selectedTrafficLightObject;
        private Lane selectedLane;
        private KeyCode keyboardKey = KeyCode.Alpha1;
        private bool laneOpenOnAssign = true;
        private Lane[] selectedLanes = new Lane[0];

        [MenuItem("Tools/Traffic Light Tool")]
        public static void Open()
        {
            GetWindow<TrafficLightManagerWindow>("Traffic Light Tool");
        }

        private void OnGUI()
        {
            PullFromSelection();

            selectedTrafficLightObject = (GameObject)EditorGUILayout.ObjectField("Selected Traffic Light", selectedTrafficLightObject, typeof(GameObject), true);
            selectedLane = (Lane)EditorGUILayout.ObjectField("Selected Lane", selectedLane, typeof(Lane), true);
            EditorGUILayout.LabelField("Selected Lanes", selectedLanes.Length.ToString());
            keyboardKey = (KeyCode)EditorGUILayout.EnumPopup("Keyboard Key", keyboardKey);
            laneOpenOnAssign = EditorGUILayout.Toggle("Lanes Open", laneOpenOnAssign);

            if (selectedTrafficLightObject == null)
            {
                EditorGUILayout.HelpBox("Select a traffic light object from scene.", MessageType.Warning);
            }
            if (selectedLanes.Length == 0)
            {
                EditorGUILayout.HelpBox("Select one or more lanes from scene.", MessageType.Warning);
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("ASSIGN", GUILayout.Height(34f)))
            {
                Assign();
            }
        }

        private void Assign()
        {
            Lane[] lanesToAssign = GetLanesToAssign();
            if (selectedTrafficLightObject == null || lanesToAssign.Length == 0)
            {
                ShowNotification(new GUIContent("Select traffic light + lanes first."));
                return;
            }

            TrafficLightController light = selectedTrafficLightObject.GetComponent<TrafficLightController>();
            if (light == null)
            {
                Undo.RegisterCompleteObjectUndo(selectedTrafficLightObject, "Add Traffic Light Controller");
                light = Undo.AddComponent<TrafficLightController>(selectedTrafficLightObject);
            }

            Undo.RecordObject(light, "Assign Traffic Light Key");
            light.SetKeyboardToggleKey(keyboardKey);
            if (laneOpenOnAssign) { light.SetGreen(); } else { light.SetRed(); }
            EditorUtility.SetDirty(light);

            for (int i = 0; i < lanesToAssign.Length; i++)
            {
                Lane lane = lanesToAssign[i];
                if (lane == null) { continue; }

                Undo.RecordObject(lane, "Assign Lane To Traffic Light");
                lane.RefreshWaypointsFromChildren();
                int stopIndex = Mathf.Clamp(lane.Waypoints.Count - 2, 0, Mathf.Max(0, lane.Waypoints.Count - 1));
                lane.SetTrafficLight(light, stopIndex);
                EditorUtility.SetDirty(lane);
            }
        }

        private void PullFromSelection()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0) { return; }

            List<Lane> lanes = new List<Lane>();
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i];
                if (go == null) { continue; }

                Lane lane = go.GetComponent<Lane>();
                if (lane != null)
                {
                    if (!lanes.Contains(lane)) { lanes.Add(lane); }
                    continue;
                }

                if (selectedTrafficLightObject == null &&
                    (go.GetComponent<TrafficLightController>() != null || go.GetComponent<Renderer>() != null || go.GetComponentInChildren<Renderer>() != null))
                {
                    selectedTrafficLightObject = go;
                    TrafficLightController existing = go.GetComponent<TrafficLightController>();
                    if (existing != null && existing.KeyboardToggleKey != KeyCode.None)
                    {
                        keyboardKey = existing.KeyboardToggleKey;
                    }
                }
            }

            selectedLanes = lanes.ToArray();

            if (selectedLane == null && selectedLanes.Length == 1)
            {
                selectedLane = selectedLanes[0];
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private Lane[] GetLanesToAssign()
        {
            if (selectedLane != null)
            {
                return new[] { selectedLane };
            }

            return selectedLanes ?? new Lane[0];
        }
    }
}
#endif
