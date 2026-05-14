#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using MyTrafficSystem.TrafficLights;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class PedestrianTrafficSystemWindow : EditorWindow
    {
        private enum Mode { None, CreatePath, ExtendPath }

        private static Mode mode;
        private static PedestrianLane activePath;
        private static int extendInsertIndex = -1;

        private PedestrianLane pathA;
        private PedestrianLane pathB;
        private PedestrianLane splitTarget;
        private int splitIndex = 1;
        private string renameText = "";
        private bool previewSplit = true;

        private PedestrianSpawner spawner;
        private PedestrianCrosswalkNode crosswalkNode;
        private TrafficLightGroup crosswalkGroup;

        [MenuItem("Tools/Pedestrian Traffic System")]
        public static void Open() => GetWindow<PedestrianTrafficSystemWindow>("Pedestrian Traffic System");

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGui;
            if (spawner == null) spawner = FindFirstObjectByType<PedestrianSpawner>();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            mode = Mode.None;
            activePath = null;
        }

        private void OnGUI()
        {
            DrawPathSection();
            EditorGUILayout.Space(8f);
            DrawCrosswalkSection();
            EditorGUILayout.Space(8f);
            DrawSpawnerSection();
            EditorGUILayout.Space(8f);
            DrawDebugSection();
        }

        private void DrawPathSection()
        {
            EditorGUILayout.LabelField("Path Creation", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Path", GUILayout.Height(28f))) StartCreatePath();
            if (GUILayout.Button("Extend Path", GUILayout.Height(28f))) StartExtendPath();

            pathA = (PedestrianLane)EditorGUILayout.ObjectField("Path A", pathA, typeof(PedestrianLane), true);
            pathB = (PedestrianLane)EditorGUILayout.ObjectField("Path B", pathB, typeof(PedestrianLane), true);
            if (GUILayout.Button("Connect Paths", GUILayout.Height(24f))) ConnectPaths();

            splitTarget = (PedestrianLane)EditorGUILayout.ObjectField("Break Path Target", splitTarget, typeof(PedestrianLane), true);
            previewSplit = EditorGUILayout.Toggle("Preview Break", previewSplit);
            if (splitTarget != null)
            {
                splitTarget.RefreshWaypointsFromChildren();
                int max = Mathf.Max(1, splitTarget.Waypoints.Count - 2);
                splitIndex = EditorGUILayout.IntSlider("Break At Waypoint", Mathf.Clamp(splitIndex, 1, max), 1, max);
            }
            if (GUILayout.Button("Break Path", GUILayout.Height(24f))) BreakPath();

            renameText = EditorGUILayout.TextField("Rename Selected Path", renameText);
            if (GUILayout.Button("Rename Path", GUILayout.Height(24f))) RenameSelectedPath();
            if (GUILayout.Button("Delete Selected Path", GUILayout.Height(24f))) DeleteSelectedPath();
            if (GUILayout.Button("Exit Edit Mode", GUILayout.Height(24f))) ExitMode();
        }

        private void DrawCrosswalkSection()
        {
            EditorGUILayout.LabelField("Crosswalk Setup", EditorStyles.boldLabel);
            crosswalkNode = (PedestrianCrosswalkNode)EditorGUILayout.ObjectField("Crosswalk Node", crosswalkNode, typeof(PedestrianCrosswalkNode), true);
            crosswalkGroup = (TrafficLightGroup)EditorGUILayout.ObjectField("Traffic Group", crosswalkGroup, typeof(TrafficLightGroup), true);
            if (GUILayout.Button("Assign Group To Crosswalk", GUILayout.Height(24f)))
            {
                if (crosswalkNode != null)
                {
                    Undo.RecordObject(crosswalkNode, "Assign Crosswalk Group");
                    SerializedObject so = new SerializedObject(crosswalkNode);
                    so.FindProperty("linkedTrafficGroup").objectReferenceValue = crosswalkGroup;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(crosswalkNode);
                }
            }
        }

        private void DrawSpawnerSection()
        {
            EditorGUILayout.LabelField("Pedestrian Spawning", EditorStyles.boldLabel);
            spawner = (PedestrianSpawner)EditorGUILayout.ObjectField("Spawner", spawner, typeof(PedestrianSpawner), true);
            if (spawner == null)
            {
                if (GUILayout.Button("Create Pedestrian Spawner", GUILayout.Height(24f)))
                {
                    GameObject go = new GameObject("PedestrianSpawner");
                    Undo.RegisterCreatedObjectUndo(go, "Create Pedestrian Spawner");
                    spawner = Undo.AddComponent<PedestrianSpawner>(go);
                    Selection.activeGameObject = go;
                }
            }
            else
            {
                SerializedObject so = new SerializedObject(spawner);
                EditorGUILayout.PropertyField(so.FindProperty("pedestrianPrefabs"), true);
                EditorGUILayout.PropertyField(so.FindProperty("spawnPaths"), true);
                EditorGUILayout.PropertyField(so.FindProperty("maxPedestrians"));
                EditorGUILayout.PropertyField(so.FindProperty("minSpawnInterval"));
                EditorGUILayout.PropertyField(so.FindProperty("maxSpawnInterval"));
                so.ApplyModifiedProperties();
            }
        }

        private static void DrawDebugSection()
        {
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            PedestrianDebugSettings.ShowDebug = EditorGUILayout.Toggle("Show Debug", PedestrianDebugSettings.ShowDebug);
            PedestrianDebugSettings.ShowPathLabels = EditorGUILayout.Toggle("Show Path Labels", PedestrianDebugSettings.ShowPathLabels);
            PedestrianDebugSettings.ShowCrosswalkLinks = EditorGUILayout.Toggle("Show Crosswalk Links", PedestrianDebugSettings.ShowCrosswalkLinks);
        }

        private static void OnSceneGui(SceneView view)
        {
            Event e = Event.current;
            if (e.alt || e.button == 1 || e.button == 2 || e.type == EventType.ScrollWheel) return;

            if (mode == Mode.CreatePath) HandleCreatePath(e);
            else if (mode == Mode.ExtendPath) HandleExtendPath(e);

            PedestrianTrafficSystemWindow window = GetWindow<PedestrianTrafficSystemWindow>();
            if (window != null) window.DrawSplitPreview();
        }

        private static void HandleCreatePath(Event e)
        {
            if (activePath == null)
            {
                mode = Mode.None;
                return;
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Escape))
            {
                activePath.RefreshWaypointsFromChildren();
                mode = Mode.None;
                activePath = null;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                CreateWaypoint(activePath, GetPointFromMouse(e.mousePosition));
                e.Use();
            }
        }

        private static void HandleExtendPath(Event e)
        {
            if (activePath == null)
            {
                mode = Mode.None;
                return;
            }

            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Escape))
            {
                activePath.RefreshWaypointsFromChildren();
                mode = Mode.None;
                activePath = null;
                extendInsertIndex = -1;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                InsertOrAppendWaypoint(activePath, GetPointFromMouse(e.mousePosition));
                e.Use();
            }
        }

        private static void StartCreatePath()
        {
            GameObject go = new GameObject($"PedestrianPath_{Object.FindObjectsByType<PedestrianLane>(FindObjectsSortMode.None).Length + 1:00}");
            Undo.RegisterCreatedObjectUndo(go, "Create Pedestrian Path");
            if (SceneView.lastActiveSceneView != null) go.transform.position = SceneView.lastActiveSceneView.pivot;
            PedestrianLane lane = Undo.AddComponent<PedestrianLane>(go);
            lane.PathName = go.name;
            mode = Mode.CreatePath;
            activePath = lane;
            Selection.activeGameObject = go;
        }

        private static void StartExtendPath()
        {
            if (Selection.activeGameObject == null) return;

            PedestrianLane lane = Selection.activeGameObject.GetComponent<PedestrianLane>();
            if (lane == null)
            {
                PedestrianWaypoint wp = Selection.activeGameObject.GetComponent<PedestrianWaypoint>();
                if (wp != null) lane = wp.GetComponentInParent<PedestrianLane>();
            }

            if (lane == null) return;

            lane.RefreshWaypointsFromChildren();
            activePath = lane;
            mode = Mode.ExtendPath;
            extendInsertIndex = ResolveInsertIndex(lane);
            Selection.activeGameObject = lane.gameObject;
        }

        private static void CreateWaypoint(PedestrianLane lane, Vector3 pos)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Create Pedestrian Waypoint");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.position = pos;
            PedestrianWaypoint wp = Undo.AddComponent<PedestrianWaypoint>(wpObj);
            Undo.RecordObject(lane, "Add Pedestrian Waypoint");
            lane.AddWaypoint(wp);
            EditorUtility.SetDirty(lane);
        }

        private static void InsertOrAppendWaypoint(PedestrianLane lane, Vector3 pos)
        {
            GameObject wpObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(wpObj, "Extend Pedestrian Path");
            wpObj.transform.SetParent(lane.transform);
            wpObj.transform.position = pos;
            PedestrianWaypoint wp = Undo.AddComponent<PedestrianWaypoint>(wpObj);
            Undo.RecordObject(lane, "Extend Pedestrian Path");

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

        private static int ResolveInsertIndex(PedestrianLane lane)
        {
            PedestrianWaypoint wp = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<PedestrianWaypoint>() : null;
            if (wp == null) return lane.Waypoints.Count;

            for (int i = 0; i < lane.Waypoints.Count; i++)
            {
                if (lane.Waypoints[i] == wp) return i + 1;
            }
            return lane.Waypoints.Count;
        }

        private void ConnectPaths()
        {
            if (pathA == null || pathB == null || pathA == pathB) return;
            Undo.RecordObject(pathA, "Connect Pedestrian Paths");
            pathA.ConnectTo(pathB);

            GameObject go = new GameObject($"PedConn_{pathA.name}_to_{pathB.name}");
            Undo.RegisterCreatedObjectUndo(go, "Create Pedestrian Connection");
            PedestrianLaneConnection conn = Undo.AddComponent<PedestrianLaneConnection>(go);
            conn.TryAssign(pathA, pathB);
            EditorUtility.SetDirty(pathA);
        }

        private void BreakPath()
        {
            if (splitTarget == null) return;
            splitTarget.RefreshWaypointsFromChildren();
            if (splitTarget.Waypoints.Count < 3) return;

            int idx = Mathf.Clamp(splitIndex, 1, splitTarget.Waypoints.Count - 2);
            PedestrianWaypoint splitWp = splitTarget.Waypoints[idx];
            if (splitWp == null) return;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Break Pedestrian Path");

            PedestrianLane original = splitTarget;
            PedestrianLane[] oldConnections = new PedestrianLane[original.ConnectedLanes.Count];
            for (int i = 0; i < original.ConnectedLanes.Count; i++) oldConnections[i] = original.ConnectedLanes[i];

            GameObject bObj = new GameObject($"{original.PathName}_B");
            Undo.RegisterCreatedObjectUndo(bObj, "Create Split Path B");
            bObj.transform.SetParent(original.transform.parent);
            bObj.transform.position = original.transform.position;
            PedestrianLane laneB = Undo.AddComponent<PedestrianLane>(bObj);

            original.RenamePath($"{original.PathName}_A");
            laneB.RenamePath($"{original.PathName}_B");

            GameObject splitCloneObj = new GameObject();
            Undo.RegisterCreatedObjectUndo(splitCloneObj, "Create Split Continuity Waypoint");
            splitCloneObj.transform.SetParent(laneB.transform);
            splitCloneObj.transform.position = splitWp.transform.position;
            splitCloneObj.transform.rotation = splitWp.transform.rotation;
            PedestrianWaypoint splitClone = Undo.AddComponent<PedestrianWaypoint>(splitCloneObj);
            laneB.AddWaypoint(splitClone);

            for (int i = idx + 1; i < original.Waypoints.Count; i++)
            {
                PedestrianWaypoint wp = original.Waypoints[i];
                if (wp == null) continue;
                Undo.SetTransformParent(wp.transform, laneB.transform, "Move Waypoint To Path B");
                laneB.AddWaypoint(wp);
            }

            original.RefreshWaypointsFromChildren();
            laneB.RefreshWaypointsFromChildren();

            original.ConnectTo(laneB);
            for (int i = 0; i < oldConnections.Length; i++)
            {
                PedestrianLane target = oldConnections[i];
                if (target == null || target == laneB) continue;
                laneB.ConnectTo(target);
                original.RemoveConnectionTo(target);
            }

            PedestrianLaneConnection[] allConnections = Object.FindObjectsByType<PedestrianLaneConnection>(FindObjectsSortMode.None);
            for (int i = 0; i < allConnections.Length; i++)
            {
                PedestrianLaneConnection conn = allConnections[i];
                if (conn == null || conn.FromLane != original) continue;
                PedestrianLane to = conn.ToLane;
                if (to == null || to == laneB) continue;
                Undo.RecordObject(conn, "Reassign Split Path Connection");
                conn.TryAssign(laneB, to);
                EditorUtility.SetDirty(conn);
            }

            Selection.objects = new Object[] { original.gameObject, laneB.gameObject };
            Undo.CollapseUndoOperations(group);
        }

        private void RenameSelectedPath()
        {
            if (Selection.activeGameObject == null || string.IsNullOrWhiteSpace(renameText)) return;
            PedestrianLane lane = Selection.activeGameObject.GetComponent<PedestrianLane>();
            if (lane == null) return;
            Undo.RecordObject(lane.gameObject, "Rename Pedestrian Path");
            lane.RenamePath(renameText);
            EditorUtility.SetDirty(lane);
        }

        private void DeleteSelectedPath()
        {
            if (Selection.activeGameObject == null) return;
            PedestrianLane lane = Selection.activeGameObject.GetComponent<PedestrianLane>();
            if (lane == null) return;
            Undo.DestroyObjectImmediate(lane.gameObject);
        }

        private static void ExitMode()
        {
            mode = Mode.None;
            activePath = null;
            extendInsertIndex = -1;
        }

        private void DrawSplitPreview()
        {
            if (!previewSplit || splitTarget == null) return;
            splitTarget.RefreshWaypointsFromChildren();
            if (splitTarget.Waypoints.Count < 3) return;

            int idx = Mathf.Clamp(splitIndex, 1, splitTarget.Waypoints.Count - 2);
            PedestrianWaypoint wp = splitTarget.Waypoints[idx];
            if (wp == null) return;

            Handles.color = Color.magenta;
            Handles.SphereHandleCap(0, wp.transform.position + Vector3.up * 0.2f, Quaternion.identity, 0.5f, EventType.Repaint);
            Handles.Label(wp.transform.position + Vector3.up * 1.1f, $"Break @{idx}");

            if (PedestrianDebugSettings.ShowPathLabels)
            {
                Handles.color = splitTarget.DebugColor;
                Handles.Label(splitTarget.transform.position + Vector3.up * 0.6f, splitTarget.PathName);
            }
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
