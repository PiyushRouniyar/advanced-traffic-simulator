using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Pedestrians
{
    [DisallowMultipleComponent]
    public class PedestrianSpawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> pedestrianPrefabs = new List<GameObject>();
        [SerializeField] private List<PedestrianLane> spawnLanes = new List<PedestrianLane>();
        [SerializeField] private int maxPedestrians = 60;
        [SerializeField] private float spawnInterval = 1.6f;
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private float spawnBlockRadius = 0.8f;

        private readonly List<GameObject> activePedestrians = new List<GameObject>();
        private float timer;
        private bool running;

        public List<GameObject> PedestrianPrefabs => pedestrianPrefabs;
        public List<PedestrianLane> SpawnLanes => spawnLanes;
        public int ActiveCount => activePedestrians.Count;

        public void AddSpawnLane(PedestrianLane lane)
        {
            if (lane == null)
            {
                return;
            }

            if (spawnLanes == null)
            {
                spawnLanes = new List<PedestrianLane>();
            }

            if (!spawnLanes.Contains(lane))
            {
                spawnLanes.Add(lane);
            }
        }

        public void AddSpawnPath(CitizenPath path)
        {
            AddSpawnLane(path);
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartSpawning();
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            Cleanup();

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = Mathf.Max(0.2f, spawnInterval);
            TrySpawn();
        }

        public void StartSpawning()
        {
            running = true;
            timer = 0f;
        }

        public void StopSpawning()
        {
            running = false;
        }

        private void TrySpawn()
        {
            if (activePedestrians.Count >= Mathf.Max(1, maxPedestrians))
            {
                return;
            }

            PedestrianLane lane = GetRandomLane();
            if (lane == null || lane.StartWaypoint == null)
            {
                return;
            }

            Vector3 pos = lane.StartWaypoint.transform.position;
            if (IsBlocked(pos))
            {
                return;
            }

            GameObject prefab = GetRandomPrefab();
            if (prefab == null)
            {
                return;
            }

            GameObject ped = Instantiate(prefab, pos, Quaternion.identity);
            PedestrianAI ai = ped.GetComponent<PedestrianAI>();
            if (ai == null)
            {
                ai = ped.AddComponent<PedestrianAI>();
            }
            ai.SetStartLane(lane);
            activePedestrians.Add(ped);
        }

        private PedestrianLane GetRandomLane()
        {
            if (spawnLanes == null || spawnLanes.Count == 0) { return null; }
            List<PedestrianLane> valid = new List<PedestrianLane>();
            for (int i = 0; i < spawnLanes.Count; i++)
            {
                if (spawnLanes[i] != null && spawnLanes[i].StartWaypoint != null)
                {
                    valid.Add(spawnLanes[i]);
                }
            }
            if (valid.Count == 0) { return null; }
            return valid[Random.Range(0, valid.Count)];
        }

        private GameObject GetRandomPrefab()
        {
            if (pedestrianPrefabs == null || pedestrianPrefabs.Count == 0) { return null; }
            List<GameObject> valid = new List<GameObject>();
            for (int i = 0; i < pedestrianPrefabs.Count; i++)
            {
                if (pedestrianPrefabs[i] != null)
                {
                    valid.Add(pedestrianPrefabs[i]);
                }
            }
            if (valid.Count == 0) { return null; }
            return valid[Random.Range(0, valid.Count)];
        }

        private bool IsBlocked(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, Mathf.Max(0.2f, spawnBlockRadius), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].GetComponentInParent<PedestrianAI>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void Cleanup()
        {
            for (int i = activePedestrians.Count - 1; i >= 0; i--)
            {
                if (activePedestrians[i] == null)
                {
                    activePedestrians.RemoveAt(i);
                }
            }
        }
    }
}
