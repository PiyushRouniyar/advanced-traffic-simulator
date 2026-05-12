#if UNITY_EDITOR
using System.Collections.Generic;
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class CitizenSpawnerWindow : EditorWindow
    {
        private PedestrianSpawner spawner;
        private readonly List<CitizenLane> selectedLanes = new List<CitizenLane>();

        [MenuItem("Tools/Citizen Spawner")]
        public static void Open()
        {
            GetWindow<CitizenSpawnerWindow>("Citizen Spawner");
        }

        private void OnEnable()
        {
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<PedestrianSpawner>();
            }
            CollectSelectedPaths();
        }

        private void OnGUI()
        {
            if (spawner == null)
            {
                if (GUILayout.Button("Create Citizen Spawner", GUILayout.Height(30f)))
                {
                    GameObject go = new GameObject("CitizenSpawner");
                    Undo.RegisterCreatedObjectUndo(go, "Create Citizen Spawner");
                    spawner = Undo.AddComponent<PedestrianSpawner>(go);
                    Selection.activeGameObject = go;
                }
                return;
            }

            SerializedObject so = new SerializedObject(spawner);
            EditorGUILayout.PropertyField(so.FindProperty("pedestrianPrefabs"), new GUIContent("Citizen Prefabs"), true);
            EditorGUILayout.PropertyField(so.FindProperty("spawnLanes"), new GUIContent("Citizen Spawn Lanes"), true);
            EditorGUILayout.PropertyField(so.FindProperty("maxPedestrians"), new GUIContent("Max Citizens"));
            EditorGUILayout.PropertyField(so.FindProperty("spawnInterval"), new GUIContent("Spawn Interval"));
            so.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Selected Citizen Lanes", selectedLanes.Count.ToString());
            if (GUILayout.Button("Use Selected Lanes", GUILayout.Height(24f)))
            {
                AddSelectedLanes();
            }

            if (GUILayout.Button("START", GUILayout.Height(34f)))
            {
                if (Application.isPlaying)
                {
                    spawner.StartSpawning();
                }
            }
            if (GUILayout.Button("STOP", GUILayout.Height(30f)))
            {
                if (Application.isPlaying)
                {
                    spawner.StopSpawning();
                }
            }
        }

        private void AddSelectedLanes()
        {
            if (spawner == null)
            {
                return;
            }

            Undo.RecordObject(spawner, "Add Citizen Spawn Lanes");
            for (int i = 0; i < selectedLanes.Count; i++)
            {
                spawner.AddSpawnPath(selectedLanes[i]);
            }
            EditorUtility.SetDirty(spawner);
        }

        private void CollectSelectedPaths()
        {
            selectedLanes.Clear();
            GameObject[] selected = Selection.gameObjects;
            if (selected == null)
            {
                return;
            }

            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i];
                if (go == null)
                {
                    continue;
                }

                CitizenLane lane = go.GetComponent<CitizenLane>();
                if (lane == null)
                {
                    lane = go.GetComponentInParent<CitizenLane>();
                }

                if (lane != null && !selectedLanes.Contains(lane))
                {
                    selectedLanes.Add(lane);
                }
            }
        }

        private void OnSelectionChange()
        {
            CollectSelectedPaths();
            Repaint();
        }
    }
}
#endif
