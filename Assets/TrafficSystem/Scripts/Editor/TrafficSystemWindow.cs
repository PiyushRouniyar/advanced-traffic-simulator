#if UNITY_EDITOR
using MyTrafficSystem.Lanes;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class TrafficSystemWindow : EditorWindow
    {
        private enum Mode { None, CreateLane }

        private static Mode mode;
        private static Lane activeLane;
        private Lane lane1;
        private Lane lane2;

        [MenuItem("Tools/Simple Traffic System")]
        public static void Open()
        {
            GetWindow<TrafficSystemWindow>("Simple Traffic System");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            mode = Mode.None;
            activeLane = null;
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Create Lane", GUILayout.Height(32f)))
            {
                StartCreateLane();
            }

            if (GUILayout.Button("Connect Lanes", GUILayout.Height(32f)))
            {
                ConnectLane1ToLane2();
            }

            lane1 = (Lane)EditorGUILayout.ObjectField("Lane 1 (From)", lane1, typeof(Lane), true);
            lane2 = (Lane)EditorGUILayout.ObjectField("Lane 2 (To)", lane2, typeof(Lane), true);

            if (GUILayout.Button("Connect Lane 1 -> Lane 2", GUILayout.Height(28f)))
            {
                ConnectLane1ToLane2();
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Exit Edit Mode", GUILayout.Height(28f)))
            {
                mode = Mode.None;
                activeLane = null;
            }
        }

        private static void StartCreateLane()
        {
            GameObject go = new GameObject($"Lane_{Object.FindObjectsByType<Lane>(FindObjectsSortMode.None).Length + 1:00}");
            Undo.RegisterCreatedObjectUndo(go, "Create Lane");

            if (Selection.activeTransform != null)
            {
                go.transform.position = Selection.activeTransform.position;
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Lane lane = Undo.AddComponent<Lane>(go);
            lane.LaneName = go.name;

            mode = Mode.CreateLane;
            activeLane = lane;
            Selection.activeGameObject = go;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            Event e = Event.current;

            // Never interfere with native scene navigation.
            if (e.alt || e.button == 1 || e.button == 2 || e.type == EventType.ScrollWheel)
            {
                return;
            }

            if (mode == Mode.CreateLane)
            {
                HandleCreateLane(e);
                return;
            }

        }

        private static void HandleCreateLane(Event e)
        {
            if (activeLane == null)
            {
                mode = Mode.None;
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
            {
                activeLane.RefreshWaypointsFromChildren();
                RecenterLaneToFirstWaypoint(activeLane);
                EditorUtility.SetDirty(activeLane);
                mode = Mode.None;
                activeLane = null;
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (activeLane.Waypoints.Count == 0)
                {
                    Undo.DestroyObjectImmediate(activeLane.gameObject);
                }

                mode = Mode.None;
                activeLane = null;
                e.Use();
                return;
            }

            // Place waypoint only on SHIFT + LEFT CLICK in create mode.
            if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.control && !e.command)
            {
                Vector3 position = GetPointFromMouse(e.mousePosition);
                CreateWaypoint(activeLane, position);
                e.Use();
            }
        }

        private void ConnectLane1ToLane2()
        {
            if (lane1 == null || lane2 == null || lane1 == lane2)
            {
                return;
            }

            lane1.RefreshWaypointsFromChildren();
            lane2.RefreshWaypointsFromChildren();
            if (lane1.Waypoints.Count == 0 || lane2.Waypoints.Count == 0)
            {
                return;
            }

            Undo.RecordObject(lane1, "Connect Lanes");
            lane1.ConnectTo(lane2);
            EditorUtility.SetDirty(lane1);
            Selection.activeGameObject = lane1.gameObject;
        }

        private static void CreateWaypoint(Lane lane, Vector3 position)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Create Waypoint");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.position = position;

            Waypoint wp = Undo.AddComponent<Waypoint>(wpObj);
            Undo.RecordObject(lane, "Add Waypoint");
            lane.AddWaypoint(wp);
            EditorUtility.SetDirty(lane);
        }

        private static Vector3 GetPointFromMouse(Vector2 mousePos)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 5000f))
            {
                return hit.point;
            }

            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return ray.origin + ray.direction * 20f;
        }

        private static void RecenterLaneToFirstWaypoint(Lane lane)
        {
            if (lane == null || lane.Waypoints.Count == 0 || lane.Waypoints[0] == null)
            {
                return;
            }

            Transform laneTransform = lane.transform;
            Vector3 newLanePosition = lane.Waypoints[0].transform.position;
            if ((laneTransform.position - newLanePosition).sqrMagnitude < 0.000001f)
            {
                return;
            }

            int childCount = laneTransform.childCount;
            Vector3[] worldPositions = new Vector3[childCount];
            Quaternion[] worldRotations = new Quaternion[childCount];

            for (int i = 0; i < childCount; i++)
            {
                Transform child = laneTransform.GetChild(i);
                worldPositions[i] = child.position;
                worldRotations[i] = child.rotation;
            }

            Undo.RecordObject(laneTransform, "Recenter Lane");
            laneTransform.position = newLanePosition;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = laneTransform.GetChild(i);
                Undo.RecordObject(child, "Keep Waypoint World Position");
                child.position = worldPositions[i];
                child.rotation = worldRotations[i];
            }
        }
    }
}
#endif
