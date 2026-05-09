using System.Collections.Generic;
using MyTrafficSystem.AI;
using MyTrafficSystem.Lanes;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    [DisallowMultipleComponent]
    public class SimpleTrafficSpawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> carPrefabs = new List<GameObject>();
        [SerializeField] private Lane spawnLane;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int maxCars = 20;

        private readonly List<GameObject> cars = new List<GameObject>();
        private float timer;

        private void Update()
        {
            CleanupCars();

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = Mathf.Max(0.2f, spawnInterval);

            if (spawnLane == null || spawnLane.StartWaypoint == null)
            {
                return;
            }

            if (cars.Count >= Mathf.Max(1, maxCars) || carPrefabs.Count == 0)
            {
                return;
            }

            SpawnCar();
        }

        private void SpawnCar()
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
            if (prefab == null)
            {
                return;
            }

            Vector3 pos = spawnLane.StartWaypoint.transform.position;
            Quaternion rot = Quaternion.identity;

            if (spawnLane.Waypoints.Count > 1 && spawnLane.Waypoints[1] != null)
            {
                Vector3 dir = (spawnLane.Waypoints[1].transform.position - pos).normalized;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    rot = Quaternion.LookRotation(dir, Vector3.up);
                }
            }

            GameObject car = Instantiate(prefab, pos, rot);
            TrafficCarAI ai = car.GetComponent<TrafficCarAI>();
            if (ai == null)
            {
                ai = car.AddComponent<TrafficCarAI>();
            }

            ai.SetStartLane(spawnLane);
            cars.Add(car);
        }

        private void CleanupCars()
        {
            for (int i = cars.Count - 1; i >= 0; i--)
            {
                if (cars[i] == null)
                {
                    cars.RemoveAt(i);
                }
            }
        }
    }
}
