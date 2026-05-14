#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class CitizenLaneMakerWindow : EditorWindow
    {
        private enum EditMode { None, CreateLane, ExtendLane }

        private static EditMode mode;
        private static CitizenLane activeLane;
        private static int extendInsertIndex = -1;
        private static bool pendingPlacementClick;
        private static Vector2 pendingMouseDownPos;
        private const float ClickDragThreshold = 6f;

        private CitizenLane laneA;
        private CitizenLane laneB;
        private CitizenLane splitTarget;
        private int splitIndex = 1;
        private string renameText = string.Empty;

        [MenuItem("Tools/Citizen Lane Maker")]
        public static void Open() => GetWindow<CitizenLaneMakerWindow>("Citizen Lane Maker");

        private void OnEnable() => SceneView.duringSceneGui += OnSceneGui;
        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGui;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Citizen Lane Editing", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Lane", GUILayout.Height(28f))) StartCreateLane();
            if (GUILayout.Button("Extend Lane", GUILayout.Height(28f))) StartExtendLane();

            laneA = (CitizenLane)EditorGUILayout.ObjectField("Lane A", laneA, typeof(CitizenLane), true);
            laneB = (CitizenLane)EditorGUILayout.ObjectField("Lane B", laneB, typeof(CitizenLane), true);
            if (GUILayout.Button("Connect Lanes", GUILayout.Height(24f))) ConnectLanes();

            splitTarget = (CitizenLane)EditorGUILayout.ObjectField("Split Lane", splitTarget, typeof(CitizenLane), true);
            if (splitTarget != null)
            {
                splitTarget.RefreshWaypointsFromChildren();
                int max = Mathf.Max(1, splitTarget.Waypoints.Count - 2);
                splitIndex = EditorGUILayout.IntSlider("Split At Waypoint", Mathf.Clamp(splitIndex, 1, max), 1, max);
            }
            if (GUILayout.Button("Split Lane", GUILayout.Height(24f))) SplitLane();

            renameText = EditorGUILayout.TextField("Rename Lane", renameText);
            if (GUILayout.Button("Rename Selected Lane", GUILayout.Height(24f))) RenameSelectedLane();
            if (GUILayout.Button("Delete Selected Lane", GUILayout.Height(24f))) DeleteSelectedLane();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            CitizenDebugSettings.ShowDebug = EditorGUILayout.Toggle("Show Debug", CitizenDebugSettings.ShowDebug);
            CitizenDebugSettings.ShowLaneLabels = EditorGUILayout.Toggle("Show Lane Labels", CitizenDebugSettings.ShowLaneLabels);
            CitizenDebugSettings.ShowWaypointNodes = EditorGUILayout.Toggle("Show Waypoint Nodes", CitizenDebugSettings.ShowWaypointNodes);
            CitizenDebugSettings.ShowTrafficAssignments = EditorGUILayout.Toggle("Show Connections", CitizenDebugSettings.ShowTrafficAssignments);
            CitizenDebugSettings.ShowWaitingCitizens = EditorGUILayout.Toggle("Show Waiting Citizens", CitizenDebugSettings.ShowWaitingCitizens);

            if (GUILayout.Button("Exit Edit Mode", GUILayout.Height(24f))) ExitMode();
        }

        private static void OnSceneGui(SceneView view)
        {
            Event e = Event.current;
            if (mode == EditMode.None || activeLane == null)
            {
                pendingPlacementClick = false;
                return;
            }

            if (IsSceneNavigationEvent(e))
            {
                pendingPlacementClick = false;
                return;
            }

            if (mode == EditMode.CreateLane) HandleCreateLane(e);
            else if (mode == EditMode.ExtendLane) HandleExtendLane(e);
        }

        private static void HandleCreateLane(Event e)
        {
            if (activeLane == null) { mode = EditMode.None; return; }
            if (HandleExitKeys(e)) return;

            if (!IsIntentionalPlacementModifier(e))
            {
                pendingPlacementClick = false;
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                pendingPlacementClick = true;
                pendingMouseDownPos = e.mousePosition;
                return;
            }

            if (e.type == EventType.MouseDrag && pendingPlacementClick)
            {
                if (Vector2.Distance(pendingMouseDownPos, e.mousePosition) > ClickDragThreshold) pendingPlacementClick = false;
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && pendingPlacementClick)
            {
                pendingPlacementClick = false;
                CreateWaypoint(activeLane, GetPointFromMouse(e.mousePosition));
                e.Use();
            }
        }

        private static void HandleExtendLane(Event e)
        {
            if (activeLane == null) { mode = EditMode.None; return; }
            if (HandleExitKeys(e)) return;

            if (!IsIntentionalPlacementModifier(e))
            {
                pendingPlacementClick = false;
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                pendingPlacementClick = true;
                pendingMouseDownPos = e.mousePosition;
                return;
            }

            if (e.type == EventType.MouseDrag && pendingPlacementClick)
            {
                if (Vector2.Distance(pendingMouseDownPos, e.mousePosition) > ClickDragThreshold) pendingPlacementClick = false;
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && pendingPlacementClick)
            {
                pendingPlacementClick = false;
                InsertOrAppendWaypoint(activeLane, GetPointFromMouse(e.mousePosition));
                e.Use();
            }
        }

        private static void StartCreateLane()
        {
            GameObject go = new GameObject($"CitizenLane_{Object.FindObjectsByType<CitizenLane>(FindObjectsSortMode.None).Length + 1:00}");
            Undo.RegisterCreatedObjectUndo(go, "Create Citizen Lane");
            CitizenLane lane = Undo.AddComponent<CitizenLane>(go);
            lane.RenameLane(go.name);
            Selection.activeGameObject = go;
            activeLane = lane;
            mode = EditMode.CreateLane;
        }

        private static void StartExtendLane()
        {
            if (Selection.activeGameObject == null) return;
            CitizenLane lane = Selection.activeGameObject.GetComponent<CitizenLane>();
            if (lane == null)
            {
                CitizenWaypoint wp = Selection.activeGameObject.GetComponent<CitizenWaypoint>();
                if (wp != null) lane = wp.GetComponentInParent<CitizenLane>();
            }
            if (lane == null) return;

            lane.RefreshWaypointsFromChildren();
            activeLane = lane;
            mode = EditMode.ExtendLane;
            extendInsertIndex = ResolveInsertIndex(lane);
            Selection.activeGameObject = lane.gameObject;
        }

        private static void CreateWaypoint(CitizenLane lane, Vector3 pos)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Create Citizen Waypoint");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.position = pos;
            CitizenWaypoint wp = Undo.AddComponent<CitizenWaypoint>(wpObj);
            Undo.RecordObject(lane, "Add Citizen Waypoint");
            lane.AddWaypoint(wp);
            EditorUtility.SetDirty(lane);
        }

        private static void InsertOrAppendWaypoint(CitizenLane lane, Vector3 pos)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Extend Citizen Lane");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.position = pos;
            CitizenWaypoint wp = Undo.AddComponent<CitizenWaypoint>(wpObj);
            Undo.RecordObject(lane, "Extend Citizen Lane");

            if (extendInsertIndex >= 0 && extendInsertIndex <= lane.Waypoints.Count)
            {
                wpObj.transform.SetSiblingIndex(extendInsertIndex);
                lane.InsertWaypointAt(extendInsertIndex, wp);
                extendInsertIndex++;
            }
            else
            {
                lane.AddWaypoint(wp);
            }

            EditorUtility.SetDirty(lane);
        }

        private static int ResolveInsertIndex(CitizenLane lane)
        {
            CitizenWaypoint wp = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<CitizenWaypoint>() : null;
            if (wp == null) return lane.Waypoints.Count;

            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                if (lane.Waypoints[i] == wp) return i + 1;
            }
            return lane.Waypoints.Count;
        }

        private void ConnectLanes()
        {
            if (laneA == null || laneB == null || laneA == laneB) return;
            Undo.RecordObject(laneA, "Connect Citizen Lanes");
            laneA.ConnectTo(laneB);

            GameObject go = new GameObject($"CitizenConn_{laneA.name}_to_{laneB.name}");
            Undo.RegisterCreatedObjectUndo(go, "Create Citizen Lane Connection");
            CitizenLaneConnection conn = Undo.AddComponent<CitizenLaneConnection>(go);
            conn.TryAssign(laneA, laneB);
            EditorUtility.SetDirty(laneA);
        }

        private void SplitLane()
        {
            if (splitTarget == null) return;
            splitTarget.RefreshWaypointsFromChildren();
            if (splitTarget.Waypoints.Count < 3) return;

            int idx = Mathf.Clamp(splitIndex, 1, splitTarget.Waypoints.Count - 2);
            CitizenWaypoint splitWp = splitTarget.Waypoints[idx];
            if (splitWp == null) return;

            GameObject bObj = new GameObject($"{splitTarget.LaneName}_B");
            Undo.RegisterCreatedObjectUndo(bObj, "Split Citizen Lane");
            bObj.transform.SetParent(splitTarget.transform.parent);
            CitizenLane laneB = Undo.AddComponent<CitizenLane>(bObj);
            laneB.RenameLane($"{splitTarget.LaneName}_B");

            GameObject splitCloneObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(splitCloneObj, "Create Split Continuity Waypoint");
            splitCloneObj.transform.SetParent(laneB.transform);
            splitCloneObj.transform.position = splitWp.transform.position;
            splitCloneObj.transform.rotation = splitWp.transform.rotation;
            CitizenWaypoint splitClone = Undo.AddComponent<CitizenWaypoint>(splitCloneObj);
            laneB.AddWaypoint(splitClone);

            for (int i = idx + 1; i < splitTarget.Waypoints.Count; i++)
            {
                CitizenWaypoint wp = splitTarget.Waypoints[i];
                if (wp == null) continue;
                Undo.SetTransformParent(wp.transform, laneB.transform, "Move Waypoint To Split Lane");
                laneB.AddWaypoint(wp);
            }

            splitTarget.RenameLane($"{splitTarget.LaneName}_A");
            splitTarget.RefreshWaypointsFromChildren();
            laneB.RefreshWaypointsFromChildren();
            splitTarget.ConnectTo(laneB);
        }

        private void RenameSelectedLane()
        {
            if (Selection.activeGameObject == null || string.IsNullOrWhiteSpace(renameText)) return;
            CitizenLane lane = Selection.activeGameObject.GetComponent<CitizenLane>();
            if (lane == null) return;
            Undo.RecordObject(lane, "Rename Citizen Lane");
            lane.RenameLane(renameText);
            EditorUtility.SetDirty(lane);
        }

        private static void DeleteSelectedLane()
        {
            if (Selection.activeGameObject == null) return;
            CitizenLane lane = Selection.activeGameObject.GetComponent<CitizenLane>();
            if (lane == null) return;
            Undo.DestroyObjectImmediate(lane.gameObject);
        }

        private static void ExitMode()
        {
            mode = EditMode.None;
            activeLane = null;
            extendInsertIndex = -1;
            pendingPlacementClick = false;
        }

        private static bool HandleExitKeys(Event e)
        {
            if (e.type != EventType.KeyDown) return false;
            if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter && e.keyCode != KeyCode.Escape) return false;

            if (activeLane != null) activeLane.RefreshWaypointsFromChildren();
            ExitMode();
            e.Use();
            return true;
        }

        private static bool IsIntentionalPlacementModifier(Event e) => e.shift && !e.alt && !e.control && !e.command;

        private static bool IsSceneNavigationEvent(Event e)
        {
            if (e == null) return false;
            if (Tools.viewToolActive) return true;
            if (e.alt) return true;
            if (e.type == EventType.ScrollWheel) return true;
            if (e.button == 1 || e.button == 2) return true;
            if (GUIUtility.hotControl != 0) return true;
            return false;
        }

        private static Vector3 GetPointFromMouse(Vector2 mousePos)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 5000f)) return hit.point;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter);
            return ray.origin + ray.direction * 20f;
        }
    }
}
#endif
