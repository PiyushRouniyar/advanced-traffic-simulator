#if UNITY_EDITOR
using System.Collections.Generic;
using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.TrafficLights;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class CCTVCameraToolWindow : EditorWindow
    {
        private CCTVCameraSystem runtimeSystem;
        private CCTVCameraPoint selectedPoint;

        private string cameraName = "CAM_01 Downtown";
        private string cameraGroup = "Default";
        private float cameraFov = 60f;

        [MenuItem("Tools/CCTV Camera Tool")]
        public static void Open() => GetWindow<CCTVCameraToolWindow>("CCTV Camera Tool");

        private void OnEnable()
        {
            runtimeSystem = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
        }

        private void OnGUI()
        {
            if (runtimeSystem == null)
            {
                runtimeSystem = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
                if (runtimeSystem == null && GUILayout.Button("Create CCTV Camera System", GUILayout.Height(28f)))
                {
                    GameObject go = new GameObject("CCTVSystem");
                    runtimeSystem = go.AddComponent<CCTVCameraSystem>();
                }
            }

            EditorGUILayout.LabelField("Create Camera", EditorStyles.boldLabel);
            cameraName = EditorGUILayout.TextField("Camera Name", cameraName);
            cameraGroup = EditorGUILayout.TextField("Camera Group", cameraGroup);
            cameraFov = EditorGUILayout.Slider("FOV", cameraFov, 25f, 95f);

            if (GUILayout.Button("Create CCTV Camera Point", GUILayout.Height(30f)))
            {
                CreateCameraPoint();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Edit Camera", EditorStyles.boldLabel);
            selectedPoint = (CCTVCameraPoint)EditorGUILayout.ObjectField("Selected Camera Point", selectedPoint, typeof(CCTVCameraPoint), true);

            if (selectedPoint != null)
            {
                DrawSelectedCameraEditor();
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Collect Scene Camera Points", GUILayout.Height(26f)))
            {
                runtimeSystem?.DiscoverCameraPoints();
            }
        }

        private void DrawSelectedCameraEditor()
        {
            SerializedObject so = new SerializedObject(selectedPoint);
            SerializedProperty label = so.FindProperty("cameraLabel");
            SerializedProperty group = so.FindProperty("cameraGroup");
            SerializedProperty fov = so.FindProperty("fieldOfView");
            SerializedProperty zoom = so.FindProperty("zoomFieldOfView");
            SerializedProperty allowZoom = so.FindProperty("allowZoom");

            EditorGUILayout.PropertyField(label);
            EditorGUILayout.PropertyField(group);
            EditorGUILayout.PropertyField(fov);
            EditorGUILayout.PropertyField(allowZoom);
            EditorGUILayout.PropertyField(zoom);

            so.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Camera", GUILayout.Height(24f)))
            {
                PreviewPointInSceneView(selectedPoint);
            }
            if (GUILayout.Button("Focus", GUILayout.Height(24f)))
            {
                Selection.activeGameObject = selectedPoint.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh Runtime Camera List", GUILayout.Height(24f)))
            {
                runtimeSystem?.DiscoverCameraPoints();
            }
        }

        private void CreateCameraPoint()
        {
            SceneView scene = SceneView.lastActiveSceneView;
            Vector3 pos = scene != null ? scene.pivot : Vector3.zero;
            Quaternion rot = scene != null ? scene.rotation : Quaternion.identity;

            GameObject go = new GameObject(string.IsNullOrWhiteSpace(cameraName) ? "CCTV_Camera" : cameraName.Replace(" ", "_"));
            Undo.RegisterCreatedObjectUndo(go, "Create CCTV Camera Point");
            go.transform.position = pos;
            go.transform.rotation = rot;

            CCTVCameraPoint point = Undo.AddComponent<CCTVCameraPoint>(go);
            SerializedObject so = new SerializedObject(point);
            so.FindProperty("cameraLabel").stringValue = string.IsNullOrWhiteSpace(cameraName) ? go.name : cameraName;
            so.FindProperty("cameraGroup").stringValue = string.IsNullOrWhiteSpace(cameraGroup) ? "Default" : cameraGroup;
            so.FindProperty("fieldOfView").floatValue = cameraFov;
            so.ApplyModifiedProperties();

            selectedPoint = point;
            Selection.activeGameObject = go;
            runtimeSystem?.DiscoverCameraPoints();
        }

        private static void PreviewPointInSceneView(CCTVCameraPoint point)
        {
            if (point == null || SceneView.lastActiveSceneView == null) return;

            SceneView sv = SceneView.lastActiveSceneView;
            sv.pivot = point.transform.position + point.transform.forward * 10f;
            sv.rotation = point.transform.rotation;
            sv.size = 8f;
            sv.Repaint();
        }
    }
}
#endif
