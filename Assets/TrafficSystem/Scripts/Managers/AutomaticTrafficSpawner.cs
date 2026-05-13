using System.Collections.Generic;
using MyTrafficSystem.AI;
using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    [DisallowMultipleComponent]
    public class AutomaticTrafficSpawner : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private List<GameObject> carPrefabs = new List<GameObject>();
        [SerializeField] private List<Lane> spawnLanes = new List<Lane>();

        [Header("Traffic")]
        [SerializeField] private int maxActiveCars = 100;
        [SerializeField] private float spawnInterval = 1.2f;
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private float spawnBlockRadius = 2f;

        private readonly List<GameObject> activeCars = new List<GameObject>();
        private float timer;
        private bool running;

        public List<GameObject> CarPrefabs => carPrefabs;
        public List<Lane> SpawnLanes => spawnLanes;
        public int MaxActiveCars { get => maxActiveCars; set => maxActiveCars = Mathf.Max(1, value); }
        public float SpawnInterval { get => spawnInterval; set => spawnInterval = Mathf.Max(0.2f, value); }
        public int ActiveCarCount => activeCars.Count;

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartTraffic();
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            CleanupDestroyedCars();

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = Mathf.Max(0.2f, spawnInterval);
            TrySpawn();
        }

        public void StartTraffic()
        {
            running = true;
            timer = 0f;
        }

        public void StopTraffic()
        {
            running = false;
        }

        public void SetTrafficRunning(bool isRunning)
        {
            if (isRunning) { StartTraffic(); } else { StopTraffic(); }
        }

        private void TrySpawn()
        {
            if (activeCars.Count >= Mathf.Max(1, maxActiveCars))
            {
                return;
            }

            Lane lane = GetRandomValidLane();
            if (lane == null || lane.StartWaypoint == null)
            {
                return;
            }

            if (IsSpawnBlocked(lane.StartWaypoint.transform.position))
            {
                return;
            }

            GameObject prefab = GetRandomValidPrefab();
            if (prefab == null)
            {
                return;
            }

            Vector3 position = lane.StartWaypoint.transform.position;
            Quaternion rotation = GetLaneStartRotation(lane, position);

            GameObject car = Instantiate(prefab, position, rotation);
            EnsureAiSetup(car, lane);
            activeCars.Add(car);
        }

        private void EnsureAiSetup(GameObject car, Lane lane)
        {
            if (car == null || lane == null)
            {
                return;
            }

            TrafficCarAI ai = car.GetComponent<TrafficCarAI>();
            if (ai == null)
            {
                ai = car.AddComponent<TrafficCarAI>();
            }

            if (car.GetComponent<TrafficRouteDecider>() == null)
            {
                car.AddComponent<TrafficRouteDecider>();
            }

            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = car.AddComponent<Rigidbody>();
                rb.mass = 1200f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            ai.SetStartLane(lane);
        }

        private Lane GetRandomValidLane()
        {
            if (spawnLanes == null || spawnLanes.Count == 0)
            {
                return null;
            }

            List<Lane> valid = new List<Lane>();
            for (int i = 0; i < spawnLanes.Count; i++)
            {
                Lane lane = spawnLanes[i];
                if (lane != null && lane.StartWaypoint != null)
                {
                    valid.Add(lane);
                }
            }

            if (valid.Count == 0)
            {
                return null;
            }

            return valid[Random.Range(0, valid.Count)];
        }

        private GameObject GetRandomValidPrefab()
        {
            if (carPrefabs == null || carPrefabs.Count == 0)
            {
                return null;
            }

            List<GameObject> valid = new List<GameObject>();
            for (int i = 0; i < carPrefabs.Count; i++)
            {
                if (carPrefabs[i] != null)
                {
                    valid.Add(carPrefabs[i]);
                }
            }

            if (valid.Count == 0)
            {
                return null;
            }

            return valid[Random.Range(0, valid.Count)];
        }

        private static Quaternion GetLaneStartRotation(Lane lane, Vector3 fromPosition)
        {
            if (lane.Waypoints.Count > 1 && lane.Waypoints[1] != null)
            {
                Vector3 direction = lane.Waypoints[1].transform.position - fromPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            return Quaternion.identity;
        }

        private bool IsSpawnBlocked(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, Mathf.Max(0.5f, spawnBlockRadius), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                {
                    continue;
                }

                if (hits[i].GetComponentInParent<TrafficCarAI>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CleanupDestroyedCars()
        {
            for (int i = activeCars.Count - 1; i >= 0; i--)
            {
                if (activeCars[i] == null)
                {
                    activeCars.RemoveAt(i);
                }
            }
        }
    }
}
