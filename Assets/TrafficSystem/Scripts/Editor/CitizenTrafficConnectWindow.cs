#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using MyTrafficSystem.TrafficLights;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class CitizenTrafficConnectWindow : EditorWindow
    {
        private CitizenLane targetLane;
        private MyTrafficSystem.TrafficLights.TrafficLightController targetTrafficLight;
        private int stopWaypointIndex = -1;

        [MenuItem("Tools/Citizen Traffic Connect")]
        public static void Open() => GetWindow<CitizenTrafficConnectWindow>("Citizen Traffic Connect");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Lane -> Traffic Light Assignment", EditorStyles.boldLabel);
            targetLane = (CitizenLane)EditorGUILayout.ObjectField("Citizen Lane", targetLane, typeof(CitizenLane), true);
            targetTrafficLight = (MyTrafficSystem.TrafficLights.TrafficLightController)EditorGUILayout.ObjectField(
                "Traffic Light",
                targetTrafficLight,
                typeof(MyTrafficSystem.TrafficLights.TrafficLightController),
                true);

            int maxIdx = 0;
            if (targetLane != null)
            {
                targetLane.RefreshWaypointsFromChildren();
                maxIdx = Mathf.Max(0, targetLane.Waypoints.Count - 1);
            }

            stopWaypointIndex = EditorGUILayout.IntSlider("Stop Waypoint Index", Mathf.Clamp(stopWaypointIndex, -1, maxIdx), -1, maxIdx);

            if (GUILayout.Button("Assign Connection", GUILayout.Height(30f)))
            {
                Assign();
            }

            EditorGUILayout.HelpBox("Rule: car light GREEN => citizens stop. car light RED => citizens cross.", MessageType.Info);

            CitizenDebugSettings.ShowTrafficAssignments = EditorGUILayout.Toggle("Show Assignment Debug", CitizenDebugSettings.ShowTrafficAssignments);
            CitizenDebugSettings.ShowDebug = EditorGUILayout.Toggle("Show Citizen Debug", CitizenDebugSettings.ShowDebug);
        }

        private void Assign()
        {
            if (targetLane == null) return;

            int resolvedStopIndex = stopWaypointIndex;
            if (resolvedStopIndex < 0)
            {
                targetLane.RefreshWaypointsFromChildren();
                resolvedStopIndex = Mathf.Max(0, targetLane.Waypoints.Count - 1);
            }

            Undo.RecordObject(targetLane, "Assign Citizen Traffic Light");
            targetLane.AssignTrafficLight(targetTrafficLight, resolvedStopIndex);
            EditorUtility.SetDirty(targetLane);
        }
    }
}
#endif
