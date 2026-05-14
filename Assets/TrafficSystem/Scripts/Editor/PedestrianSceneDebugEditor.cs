#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    [InitializeOnLoad]
    public static class PedestrianSceneDebugEditor
    {
        static PedestrianSceneDebugEditor()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private static void OnSceneGui(SceneView scene)
        {
            if (!PedestrianDebugSettings.ShowDebug)
            {
                return;
            }

            if (PedestrianDebugSettings.ShowPathLabels)
            {
                PedestrianLane[] lanes = Object.FindObjectsByType<PedestrianLane>(FindObjectsSortMode.None);
                for (int i = 0; i < lanes.Length; i++)
                {
                    if (lanes[i] == null) continue;
                    Handles.color = lanes[i].DebugColor;
                    Handles.Label(lanes[i].transform.position + Vector3.up * 0.7f, lanes[i].PathName);
                }
            }

            if (PedestrianDebugSettings.ShowCrosswalkLinks)
            {
                PedestrianCrosswalkNode[] nodes = Object.FindObjectsByType<PedestrianCrosswalkNode>(FindObjectsSortMode.None);
                for (int i = 0; i < nodes.Length; i++)
                {
                    PedestrianCrosswalkNode node = nodes[i];
                    if (node == null || node.LinkedTrafficGroup == null) continue;
                    Handles.color = node.CanPedestriansCross ? new Color(0.35f, 1f, 0.55f, 1f) : new Color(1f, 0.4f, 0.4f, 1f);
                    Handles.DrawLine(node.transform.position, node.LinkedTrafficGroup.transform.position);
                    Handles.Label(node.transform.position + Vector3.up * 0.35f, $"Crosswalk -> {node.LinkedTrafficGroup.GroupName}");
                }
            }
        }
    }
}
#endif
