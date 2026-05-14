using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class CitizenSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private List<GameObject> citizenPrefabs = new List<GameObject>();

        [Header("Spawn Lanes")]
        [SerializeField] private List<CitizenLane> spawnLanes = new List<CitizenLane>();

        [Header("Spawn Settings")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private int maxCitizens = 100;
        [SerializeField] private float minSpawnInterval = 1.2f;
        [SerializeField] private float maxSpawnInterval = 2.6f;
        [SerializeField] private float spawnBlockRadius = 1f;
        [SerializeField] private bool forceLoopLanes;

        private readonly List<GameObject> activeCitizens = new List<GameObject>();
        private float timer;
        private bool running;

        public int ActiveCitizenCount => activeCitizens.Count;
        public List<CitizenLane> SpawnLanes => spawnLanes;

        private void Start()
        {
            if (autoStartOnPlay) StartSpawning();
        }

        private void Update()
        {
            if (!running) return;

            Cleanup();
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            timer = Random.Range(Mathf.Max(0.2f, minSpawnInterval), Mathf.Max(minSpawnInterval, maxSpawnInterval));
            TrySpawnCitizen();
        }

        public void StartSpawning()
        {
            running = true;
            timer = 0f;
        }

        public void StopSpawning() => running = false;

        public void AddSpawnLane(CitizenLane lane)
        {
            if (lane != null && !spawnLanes.Contains(lane)) spawnLanes.Add(lane);
        }

        private void TrySpawnCitizen()
        {
            if (activeCitizens.Count >= Mathf.Max(1, maxCitizens)) return;

            CitizenLane lane = GetRandomSpawnLane();
            if (lane == null || lane.StartWaypoint == null) return;

            Vector3 spawnPos = lane.StartWaypoint.transform.position;
            if (IsBlocked(spawnPos)) return;

            GameObject prefab = GetRandomPrefab();
            if (prefab == null) return;

            GameObject citizen = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            AutoConfigureCitizen(citizen, lane);
            activeCitizens.Add(citizen);
        }

        private void AutoConfigureCitizen(GameObject citizen, CitizenLane lane)
        {
            CitizenAI ai = citizen.GetComponent<CitizenAI>();
            if (ai == null) ai = citizen.AddComponent<CitizenAI>();

            if (forceLoopLanes && lane != null)
            {
                lane.SetLoop(true);
            }

            ai.SetStartLane(lane);
        }

        private CitizenLane GetRandomSpawnLane()
        {
            List<CitizenLane> valid = new List<CitizenLane>();
            for (int i = 0; i < spawnLanes.Count; i++)
            {
                if (spawnLanes[i] != null) valid.Add(spawnLanes[i]);
            }

            if (valid.Count == 0)
            {
                CitizenLane[] all = FindObjectsByType<CitizenLane>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
            for (int i = 0; i < citizenPrefabs.Count; i++)
            {
                if (citizenPrefabs[i] != null) valid.Add(citizenPrefabs[i]);
            }

            if (valid.Count == 0) return null;
            return valid[Random.Range(0, valid.Count)];
        }

        private bool IsBlocked(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, Mathf.Max(0.2f, spawnBlockRadius), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].GetComponentInParent<CitizenAI>() != null) return true;
            }
            return false;
        }

        private void Cleanup()
        {
            for (int i = activeCitizens.Count - 1; i >= 0; i--)
            {
                if (activeCitizens[i] == null) activeCitizens.RemoveAt(i);
            }
        }
    }
}
