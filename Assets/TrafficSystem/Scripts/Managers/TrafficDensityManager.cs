using System.Collections.Generic;
using MyTrafficSystem.AI;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    public class TrafficDensityManager : MonoBehaviour
    {
        [SerializeField] private GameObject carPrefab;
        [SerializeField] private List<SpawnZone> spawnZones = new List<SpawnZone>();
        [SerializeField] private int targetCarCount = 40;
        [SerializeField] private float spawnInterval = 0.5f;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private readonly List<GameObject> activeCars = new List<GameObject>();
        private float timer;

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) { return; }
            timer = Mathf.Max(0.1f, spawnInterval);

            CleanupList();
            if (activeCars.Count >= targetCarCount || carPrefab == null || spawnZones.Count == 0) { return; }

            SpawnZone zone = spawnZones[Random.Range(0, spawnZones.Count)];
            if (zone == null || zone.SpawnLane == null) { return; }

            GameObject car = GetOrCreate();
            car.transform.position = zone.GetSpawnPosition();
            car.transform.rotation = zone.SpawnLane.StartWaypoint != null ? zone.SpawnLane.StartWaypoint.transform.rotation : Quaternion.identity;
            car.SetActive(true);

            TrafficCarAI ai = car.GetComponent<TrafficCarAI>();
            if (ai == null) { ai = car.AddComponent<TrafficCarAI>(); }
            activeCars.Add(car);
        }

        public void Despawn(GameObject car)
        {
            if (car == null) { return; }
            car.SetActive(false);
            activeCars.Remove(car);
            pool.Enqueue(car);
        }

        private GameObject GetOrCreate()
        {
            if (pool.Count > 0) { return pool.Dequeue(); }
            return Instantiate(carPrefab);
        }

        private void CleanupList()
        {
            for (int i = activeCars.Count - 1; i >= 0; i--)
            {
                if (activeCars[i] == null) { activeCars.RemoveAt(i); }
            }
        }
    }
}
