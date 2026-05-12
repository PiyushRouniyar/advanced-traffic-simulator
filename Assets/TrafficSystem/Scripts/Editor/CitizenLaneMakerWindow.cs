#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class CitizenLaneMakerWindow : EditorWindow
    {
        private enum Mode { None, CreateLane }

        private static Mode mode;
        private static CitizenLane activeLane;
        private CitizenLane lane1;
        private CitizenLane lane2;

        [MenuItem("Tools/Citizen Lane Maker")]
        public static void Open()
        {
            GetWindow<CitizenLaneMakerWindow>("Citizen Lane Maker");
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
            if (GUILayout.Button("Create Citizen Lane", GUILayout.Height(32f)))
            {
                StartCreateLane();
            }

            if (GUILayout.Button("Add Waypoint", GUILayout.Height(32f)))
            {
                AddWaypointAtSceneViewPivot();
            }

            if (GUILayout.Button("Connect Lanes", GUILayout.Height(32f)))
            {
                ConnectLane1ToLane2();
            }

            lane1 = (CitizenLane)EditorGUILayout.ObjectField("Lane 1 (From)", lane1, typeof(CitizenLane), true);
            lane2 = (CitizenLane)EditorGUILayout.ObjectField("Lane 2 (To)", lane2, typeof(CitizenLane), true);

            if (GUILayout.Button("Connect Lane 1 -> Lane 2", GUILayout.Height(28f)))
            {
                ConnectLane1ToLane2();
            }

            if (GUILayout.Button("Exit Edit Mode", GUILayout.Height(28f)))
            {
                mode = Mode.None;
                activeLane = null;
            }

            EditorGUILayout.HelpBox("Create mode: Shift + Left Click in Scene to add waypoints. Enter to finish.", MessageType.Info);
        }

        private static void StartCreateLane()
        {
            GameObject go = new GameObject($"CitizenLane_{Object.FindObjectsByType<CitizenLane>(FindObjectsSortMode.None).Length + 1:00}");
            Undo.RegisterCreatedObjectUndo(go, "Create Citizen Lane");

            if (Selection.activeTransform != null)
            {
                go.transform.position = Selection.activeTransform.position;
            }
            else if (SceneView.lastActiveSceneView != null)
            {
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            CitizenLane lane = Undo.AddComponent<CitizenLane>(go);
            mode = Mode.CreateLane;
            activeLane = lane;
            Selection.activeGameObject = go;
        }

        private static void OnSceneGui(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.alt || e.button == 1 || e.button == 2 || e.type == EventType.ScrollWheel)
            {
                return;
            }

            if (mode != Mode.CreateLane)
            {
                return;
            }

            if (activeLane == null)
            {
                mode = Mode.None;
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
            {
                activeLane.RefreshWaypointsFromChildren();
                mode = Mode.None;
                activeLane = null;
                e.Use();
                return;
            }

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                mode = Mode.None;
                activeLane = null;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.control && !e.command)
            {
                Vector3 position = GetPointFromMouse(e.mousePosition);
                CreateWaypoint(activeLane, position);
                e.Use();
            }
        }

        private void AddWaypointAtSceneViewPivot()
        {
            CitizenLane lane = ResolveActiveLane();
            if (lane == null)
            {
                return;
            }

            Vector3 position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : lane.transform.position;
            CreateWaypoint(lane, position);
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

            Undo.RecordObject(lane1, "Connect Citizen Lanes");
            lane1.ConnectTo(lane2);

            GameObject connObj = new GameObject($"CitizenLaneConn_{lane1.name}_to_{lane2.name}");
            Undo.RegisterCreatedObjectUndo(connObj, "Create Citizen Lane Connection");
            CitizenLaneConnection connection = Undo.AddComponent<CitizenLaneConnection>(connObj);
            connection.TryAssign(lane1, lane2);

            EditorUtility.SetDirty(lane1);
            Selection.activeGameObject = lane1.gameObject;
        }

        private static void CreateWaypoint(CitizenLane lane, Vector3 position)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Create Citizen Waypoint");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.position = position;
            Undo.AddComponent<CitizenWaypoint>(wpObj);
            lane.RefreshWaypointsFromChildren();
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

        private CitizenLane ResolveActiveLane()
        {
            if (activeLane != null)
            {
                return activeLane;
            }

            if (Selection.activeGameObject != null)
            {
                CitizenLane selected = Selection.activeGameObject.GetComponent<CitizenLane>();
                if (selected != null)
                {
                    return selected;
                }
            }

            return null;
        }
    }
}
#endif
