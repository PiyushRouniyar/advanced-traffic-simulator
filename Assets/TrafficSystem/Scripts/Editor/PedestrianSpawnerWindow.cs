#if UNITY_EDITOR
using System.Collections.Generic;
using MyTrafficSystem.Pedestrians;
using UnityEditor;
using UnityEngine;

namespace MyTrafficSystem.EditorTools
{
    public class PedestrianSpawnerWindow : EditorWindow
    {
        private PedestrianSpawner spawner;
        private readonly List<PedestrianLane> selectedPedestrianLanes = new List<PedestrianLane>();

        [MenuItem("Tools/Pedestrian Spawner")]
        public static void Open()
        {
            GetWindow<PedestrianSpawnerWindow>("Pedestrian Spawner");
        }

        private void OnEnable()
        {
            if (spawner == null)
            {
                spawner = FindFirstObjectByType<PedestrianSpawner>();
            }
            CollectSelectedLanes();
        }

        private void OnGUI()
        {
            if (spawner == null)
            {
                if (GUILayout.Button("Create Pedestrian Spawner", GUILayout.Height(30f)))
                {
                    GameObject go = new GameObject("PedestrianSpawner");
                    Undo.RegisterCreatedObjectUndo(go, "Create Pedestrian Spawner");
                    spawner = Undo.AddComponent<PedestrianSpawner>(go);
                    Selection.activeGameObject = go;
                }
                return;
            }

            SerializedObject so = new SerializedObject(spawner);
            EditorGUILayout.PropertyField(so.FindProperty("pedestrianPrefabs"), new GUIContent("Citizen Prefabs"), true);
            EditorGUILayout.PropertyField(so.FindProperty("spawnLanes"), new GUIContent("Pedestrian Spawn Lanes"), true);
            EditorGUILayout.PropertyField(so.FindProperty("maxPedestrians"), new GUIContent("Max Citizens"));
            EditorGUILayout.PropertyField(so.FindProperty("spawnInterval"), new GUIContent("Spawn Interval"));
            so.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Selected Pedestrian Lanes", selectedPedestrianLanes.Count.ToString());
            if (GUILayout.Button("Use Selected Lanes", GUILayout.Height(24f)))
            {
                AddSelectedLanesToSpawner();
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

            EditorGUILayout.LabelField("Active Pedestrians", spawner.ActiveCount.ToString());
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode, then click START.", MessageType.Info);
            }
        }

        private void AddSelectedLanesToSpawner()
        {
            if (spawner == null)
            {
                return;
            }

            Undo.RecordObject(spawner, "Add Pedestrian Spawn Lanes");
            for (int i = 0; i < selectedPedestrianLanes.Count; i++)
            {
                spawner.AddSpawnLane(selectedPedestrianLanes[i]);
            }
            EditorUtility.SetDirty(spawner);
        }

        private void CollectSelectedLanes()
        {
            selectedPedestrianLanes.Clear();
            GameObject[] gos = Selection.gameObjects;
            if (gos == null)
            {
                return;
            }

            for (int i = 0; i < gos.Length; i++)
            {
                GameObject go = gos[i];
                if (go == null)
                {
                    continue;
                }

                PedestrianLane lane = go.GetComponent<PedestrianLane>();
                if (lane == null)
                {
                    lane = go.GetComponentInParent<PedestrianLane>();
                }

                if (lane != null && !selectedPedestrianLanes.Contains(lane))
                {
                    selectedPedestrianLanes.Add(lane);
                }
            }
        }

        private void OnSelectionChange()
        {
            CollectSelectedLanes();
            Repaint();
        }
    }
}
#endif
