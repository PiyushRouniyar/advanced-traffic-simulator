using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private List<GameObject> pedestrianPrefabs = new List<GameObject>();

        [Header("Spawn Paths")]
        [SerializeField] private List<PedestrianLane> spawnPaths = new List<PedestrianLane>();

        [Header("Spawn Settings")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private int maxPedestrians = 80;
        [SerializeField] private float minSpawnInterval = 1.2f;
        [SerializeField] private float maxSpawnInterval = 2.8f;
        [SerializeField] private float spawnBlockRadius = 1f;

        private readonly List<GameObject> activePedestrians = new List<GameObject>();
        private float timer;
        private bool running;

        public int ActiveCount => activePedestrians.Count;

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartSpawning();
            }
        }

        private void Update()
        {
            if (!running) return;

            Cleanup();
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            timer = Random.Range(Mathf.Max(0.2f, minSpawnInterval), Mathf.Max(minSpawnInterval, maxSpawnInterval));
            TrySpawn();
        }

        public void StartSpawning()
        {
            running = true;
            timer = 0f;
        }

        public void StopSpawning() => running = false;

        public void AddSpawnPath(PedestrianLane lane)
        {
            if (lane != null && !spawnPaths.Contains(lane)) spawnPaths.Add(lane);
        }

        private void TrySpawn()
        {
            if (activePedestrians.Count >= Mathf.Max(1, maxPedestrians)) return;

            PedestrianLane lane = GetRandomSpawnPath();
            if (lane == null || lane.StartWaypoint == null) return;

            Vector3 pos = lane.StartWaypoint.transform.position;
            if (IsBlocked(pos)) return;

            GameObject prefab = GetRandomPrefab();
            if (prefab == null) return;

            GameObject ped = Instantiate(prefab, pos, Quaternion.identity, transform);
            AutoConfigurePedestrian(ped, lane);
            activePedestrians.Add(ped);
        }

        private void AutoConfigurePedestrian(GameObject ped, PedestrianLane lane)
        {
            PedestrianAI ai = ped.GetComponent<PedestrianAI>();
            if (ai == null) ai = ped.AddComponent<PedestrianAI>();
            ai.SetStartLane(lane);
        }

        private PedestrianLane GetRandomSpawnPath()
        {
            List<PedestrianLane> valid = new List<PedestrianLane>();
            for (int i = 0; i < spawnPaths.Count; i++)
            {
                if (spawnPaths[i] != null) valid.Add(spawnPaths[i]);
            }

            if (valid.Count == 0)
            {
                PedestrianLane[] all = FindObjectsByType<PedestrianLane>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null) valid.Add(all[i]);
                }
            }

            if (valid.Count == 0) return null;
            return valid[Random.Range(0, valid.Count)];
        }

        private GameObject GetRandomPrefab()
        {
            List<GameObject> valid = new List<GameObject>();
            for (int i = 0; i < pedestrianPrefabs.Count; i++)
            {
                if (pedestrianPrefabs[i] != null) valid.Add(pedestrianPrefabs[i]);
            }

            if (valid.Count == 0) return null;
            return valid[Random.Range(0, valid.Count)];
        }

        private bool IsBlocked(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, Mathf.Max(0.2f, spawnBlockRadius), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].GetComponentInParent<PedestrianAI>() != null) return true;
            }
            return false;
        }

        private void Cleanup()
        {
            for (int i = activePedestrians.Count - 1; i >= 0; i--)
            {
                if (activePedestrians[i] == null) activePedestrians.RemoveAt(i);
            }
        }
    }
}
