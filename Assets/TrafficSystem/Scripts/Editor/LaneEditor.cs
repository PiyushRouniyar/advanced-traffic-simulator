#if UNITY_EDITOR
using MyTrafficSystem.Lanes;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    [CustomEditor(typeof(Lane))]
    public class LaneEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Lane lane = (Lane)target;
            if (lane == null)
            {
                return;
            }

            if (GUILayout.Button("Refresh Waypoints"))
            {
                Undo.RecordObject(lane, "Refresh Waypoints");
                lane.RefreshWaypointsFromChildren();
                EditorUtility.SetDirty(lane);
            }
        }
    }
}
#endif
