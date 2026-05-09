using System.Collections.Generic;
using MyTrafficSystem.Vehicles;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    /// <summary>
    /// Spawns traffic vehicles using spawn points and pooled vehicle instances.
    /// </summary>
    public class TrafficSpawner : MonoBehaviour
    {
        [Header("Spawn Timing")]
        [SerializeField] private float spawnInterval = 2f;

        [Header("Vehicles")]
        [SerializeField] private List<GameObject> vehiclePrefabs = new List<GameObject>();
        [SerializeField] private int maxActiveVehicles = 120;

        [Header("Spawn Points")]
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

        [Header("Pool")]
        [SerializeField] private VehiclePool vehiclePool;

        [Header("Debug")]
        [SerializeField] private bool autoFindSpawnPointsFromChildren = true;

        private float spawnTimer;
        private readonly List<GameObject> activeVehicles = new List<GameObject>();

        private void Awake()
        {
            if (vehiclePool == null)
            {
                vehiclePool = GetComponent<VehiclePool>();
            }
        }

        private void Start()
        {
            if (vehiclePool == null)
            {
                enabled = false;
                return;
            }

            if (autoFindSpawnPointsFromChildren)
            {
                RefreshSpawnPointListFromChildren();
            }

            vehiclePool.Prewarm(vehiclePrefabs);
            spawnTimer = Mathf.Max(0.1f, spawnInterval);
        }

        private void Update()
        {
            CleanupActiveList();

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            spawnTimer = Mathf.Max(0.1f, spawnInterval);
            TrySpawnVehicle();
        }

        private void TrySpawnVehicle()
        {
            if (activeVehicles.Count >= Mathf.Max(1, maxActiveVehicles))
            {
                return;
            }

            if (!TryGetRandomPrefab(out GameObject prefab))
            {
                return;
            }

            if (!TryGetRandomValidSpawnPoint(out SpawnPoint spawnPoint))
            {
                return;
            }

            GameObject vehicle = vehiclePool.GetVehicle(prefab);
            if (vehicle == null)
            {
                return;
            }

            vehicle.transform.SetPositionAndRotation(spawnPoint.SpawnPosition, spawnPoint.SpawnRotation);

            Rigidbody rb = vehicle.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            CarWaypointFollower follower = vehicle.GetComponent<CarWaypointFollower>();
            if (follower != null && spawnPoint.AssignedPath != null)
            {
                follower.ConfigurePath(spawnPoint.AssignedPath, spawnPoint.StartingWaypointIndex);
            }

            activeVehicles.Add(vehicle);
        }

        [ContextMenu("Refresh Spawn Points From Children")]
        public void RefreshSpawnPointListFromChildren()
        {
            SpawnPoint[] found = GetComponentsInChildren<SpawnPoint>();
            spawnPoints.Clear();
            for (int i = 0; i < found.Length; i++)
            {
                spawnPoints.Add(found[i]);
            }
        }

        public void DespawnVehicle(GameObject vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            activeVehicles.Remove(vehicle);
            if (vehiclePool != null)
            {
                vehiclePool.ReturnVehicle(vehicle);
            }
        }

        private bool TryGetRandomPrefab(out GameObject prefab)
        {
            prefab = null;
            if (vehiclePrefabs.Count == 0)
            {
                return false;
            }

            List<GameObject> validPrefabs = new List<GameObject>();
            for (int i = 0; i < vehiclePrefabs.Count; i++)
            {
                if (vehiclePrefabs[i] != null)
                {
                    validPrefabs.Add(vehiclePrefabs[i]);
                }
            }

            if (validPrefabs.Count == 0)
            {
                return false;
            }

            prefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
            return true;
        }

        private bool TryGetRandomValidSpawnPoint(out SpawnPoint spawnPoint)
        {
            spawnPoint = null;
            if (spawnPoints.Count == 0)
            {
                return false;
            }

            List<SpawnPoint> validPoints = new List<SpawnPoint>();
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                SpawnPoint point = spawnPoints[i];
                if (point != null && point.CanSpawn() && point.AssignedPath != null)
                {
                    validPoints.Add(point);
                }
            }

            if (validPoints.Count == 0)
            {
                return false;
            }

            spawnPoint = validPoints[Random.Range(0, validPoints.Count)];
            return true;
        }

        private void CleanupActiveList()
        {
            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                if (activeVehicles[i] == null || !activeVehicles[i].activeInHierarchy)
                {
                    activeVehicles.RemoveAt(i);
                }
            }
        }
    }
}
