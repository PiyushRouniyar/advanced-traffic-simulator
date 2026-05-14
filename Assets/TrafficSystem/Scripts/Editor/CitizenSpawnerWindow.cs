#if UNITY_EDITOR
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class CitizenSpawnerWindow : EditorWindow
    {
        private CitizenSpawner spawner;

        [MenuItem("Tools/Citizen Spawner")]
        public static void Open() => GetWindow<CitizenSpawnerWindow>("Citizen Spawner");

        private void OnEnable() => ResolveSpawner();

        private void OnGUI()
        {
            ResolveSpawner();

            if (spawner == null)
            {
                if (GUILayout.Button("Create Citizen Spawner", GUILayout.Height(30f)))
                {
                    GameObject go = new GameObject("CitizenSpawner");
                    Undo.RegisterCreatedObjectUndo(go, "Create Citizen Spawner");
                    spawner = Undo.AddComponent<CitizenSpawner>(go);
                    Selection.activeGameObject = go;
                }
                return;
            }

            SerializedObject so = new SerializedObject(spawner);
            EditorGUILayout.LabelField("Citizen Prefabs");
            EditorGUILayout.PropertyField(so.FindProperty("citizenPrefabs"), true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Spawn Lanes");
            EditorGUILayout.PropertyField(so.FindProperty("spawnLanes"), true);

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(so.FindProperty("maxCitizens"));
            EditorGUILayout.PropertyField(so.FindProperty("minSpawnInterval"));
            EditorGUILayout.PropertyField(so.FindProperty("maxSpawnInterval"));
            EditorGUILayout.PropertyField(so.FindProperty("autoStartOnPlay"));
            EditorGUILayout.PropertyField(so.FindProperty("forceLoopLanes"));
            so.ApplyModifiedProperties();

            if (GUILayout.Button("Use Selected Lanes", GUILayout.Height(24f))) AddSelectedLanes();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("START CITIZENS", GUILayout.Height(32f)) && Application.isPlaying) spawner.StartSpawning();
            if (GUILayout.Button("STOP CITIZENS", GUILayout.Height(30f)) && Application.isPlaying) spawner.StopSpawning();

            EditorGUILayout.LabelField("Active Citizens", spawner.ActiveCitizenCount.ToString());
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode, then use START/STOP buttons.", MessageType.Info);
            }
        }

        private void ResolveSpawner()
        {
            if (spawner == null) spawner = FindFirstObjectByType<CitizenSpawner>();
        }

        private void AddSelectedLanes()
        {
            if (spawner == null) return;
            Object[] selected = Selection.objects;
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i] as GameObject;
                if (go == null) continue;
                CitizenLane lane = go.GetComponent<CitizenLane>();
                if (lane == null) continue;

                Undo.RecordObject(spawner, "Add Citizen Spawn Lane");
                spawner.AddSpawnLane(lane);
            }
            EditorUtility.SetDirty(spawner);
        }
    }
}
#endif
