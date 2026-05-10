#if UNITY_EDITOR
using MyTrafficSystem.Lanes;
using MyTrafficSystem.Managers;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class TrafficSpawnerWindow : EditorWindow
    {
        private AutomaticTrafficSpawner spawner;

        [MenuItem("Tools/Traffic Spawner")]
        public static void Open()
        {
            GetWindow<TrafficSpawnerWindow>("Traffic Spawner");
        }

        private void OnEnable()
        {
            ResolveSpawner();
        }

        private void OnGUI()
        {
            ResolveSpawner();

            if (spawner == null)
            {
                if (GUILayout.Button("Create Spawner", GUILayout.Height(30f)))
                {
                    CreateSpawner();
                }
                return;
            }

            EditorGUILayout.LabelField("Car Prefabs");
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty carPrefabs = so.FindProperty("carPrefabs");
            EditorGUILayout.PropertyField(carPrefabs, true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Spawn Lanes");
            SerializedProperty spawnLanes = so.FindProperty("spawnLanes");
            EditorGUILayout.PropertyField(spawnLanes, true);

            EditorGUILayout.Space(4f);
            SerializedProperty maxCars = so.FindProperty("maxActiveCars");
            SerializedProperty interval = so.FindProperty("spawnInterval");
            EditorGUILayout.PropertyField(maxCars, new GUIContent("Max Active Cars"));
            EditorGUILayout.PropertyField(interval, new GUIContent("Spawn Interval"));

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("START TRAFFIC", GUILayout.Height(34f)))
            {
                if (Application.isPlaying)
                {
                    spawner.StartTraffic();
                }
            }

            if (GUILayout.Button("STOP TRAFFIC", GUILayout.Height(30f)))
            {
                if (Application.isPlaying)
                {
                    spawner.StopTraffic();
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Active Cars", spawner.ActiveCarCount.ToString());

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode, then use START TRAFFIC / STOP TRAFFIC.", MessageType.Info);
            }

            if (GUILayout.Button("Use Selected Lanes", GUILayout.Height(24f)))
            {
                AddSelectedLanesToSpawner();
            }
        }

        private void ResolveSpawner()
        {
            if (spawner != null)
            {
                return;
            }

            spawner = FindFirstObjectByType<AutomaticTrafficSpawner>();
        }

        private void CreateSpawner()
        {
            GameObject go = new GameObject("AutomaticTrafficSpawner");
            Undo.RegisterCreatedObjectUndo(go, "Create Automatic Traffic Spawner");
            spawner = Undo.AddComponent<AutomaticTrafficSpawner>(go);
            Selection.activeGameObject = go;
            EditorUtility.SetDirty(go);
        }

        private void AddSelectedLanesToSpawner()
        {
            if (spawner == null)
            {
                return;
            }

            Object[] selected = Selection.objects;
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i] as GameObject;
                if (go == null)
                {
                    continue;
                }

                Lane lane = go.GetComponent<Lane>();
                if (lane != null && !spawner.SpawnLanes.Contains(lane))
                {
                    Undo.RecordObject(spawner, "Add Spawn Lane");
                    spawner.SpawnLanes.Add(lane);
                }
            }

            EditorUtility.SetDirty(spawner);
        }
    }
}
#endif
