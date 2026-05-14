#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    [InitializeOnLoad]
    public static class CitizenSceneDebugEditor
    {
        static CitizenSceneDebugEditor()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView view)
        {
            if (!CitizenDebugSettings.ShowDebug) return;

            if (CitizenDebugSettings.ShowLaneLabels)
            {
                CitizenLane[] lanes = Object.FindObjectsByType<CitizenLane>(FindObjectsSortMode.None);
                for (int i = 0; i < lanes.Length; i++)
                {
                    CitizenLane lane = lanes[i];
                    if (lane == null) continue;
                    Handles.color = lane.LaneColor;
                    Handles.Label(lane.transform.position + Vector3.up * 0.75f, lane.LaneName);
                }
            }

            if (CitizenDebugSettings.ShowTrafficAssignments)
            {
                CitizenCrossingNode[] nodes = Object.FindObjectsByType<CitizenCrossingNode>(FindObjectsSortMode.None);
                for (int i = 0; i < nodes.Length; i++)
                {
                    CitizenCrossingNode node = nodes[i];
                    if (node == null || node.LinkedTrafficGroup == null) continue;

                    Handles.color = node.CanCitizensCross ? new Color(0.35f, 1f, 0.55f, 1f) : new Color(1f, 0.4f, 0.4f, 1f);
                    Handles.DrawLine(node.transform.position, node.LinkedTrafficGroup.transform.position);
                    Handles.Label(node.transform.position + Vector3.up * 0.35f, $"Crossing -> {node.LinkedTrafficGroup.GroupName}");
                }

                CitizenLane[] lanes = Object.FindObjectsByType<CitizenLane>(FindObjectsSortMode.None);
                for (int i = 0; i < lanes.Length; i++)
                {
                    CitizenLane lane = lanes[i];
                    if (lane == null || lane.AssignedTrafficLight == null) continue;

                    bool canCross = lane.AssignedTrafficLight.CurrentState == MyTrafficSystem.TrafficLights.TrafficLightState.Red;
                    Handles.color = canCross ? new Color(0.35f, 1f, 0.55f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
                    Vector3 anchor = lane.EndWaypoint != null ? lane.EndWaypoint.transform.position : lane.transform.position;
                    Handles.DrawDottedLine(anchor, lane.AssignedTrafficLight.transform.position, 4f);
                    Handles.Label(anchor + Vector3.up * 0.35f, $"LaneLight -> {lane.AssignedTrafficLight.name}");
                }
            }
        }
    }
}
#endif
