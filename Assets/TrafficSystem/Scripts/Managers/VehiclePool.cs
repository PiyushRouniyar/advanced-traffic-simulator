using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    /// <summary>
    /// Simple reusable pool for multiple vehicle prefabs.
    /// </summary>
    public class VehiclePool : MonoBehaviour
    {
        [SerializeField] private Transform pooledVehiclesParent;
        [SerializeField] private int prewarmPerPrefab = 2;

        private readonly Dictionary<GameObject, Queue<GameObject>> pooledByPrefab = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<GameObject, GameObject> prefabByInstance = new Dictionary<GameObject, GameObject>();

        public void Prewarm(List<GameObject> vehiclePrefabs)
        {
            if (vehiclePrefabs == null)
            {
                return;
            }

            for (int i = 0; i < vehiclePrefabs.Count; i++)
            {
                GameObject prefab = vehiclePrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                for (int j = 0; j < Mathf.Max(0, prewarmPerPrefab); j++)
                {
                    GameObject instance = CreateInstance(prefab);
                    ReturnVehicle(instance);
                }
            }
        }

        public GameObject GetVehicle(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (!pooledByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pooledByPrefab[prefab] = queue;
            }

            while (queue.Count > 0)
            {
                GameObject pooledObject = queue.Dequeue();
                if (pooledObject == null)
                {
                    continue;
                }

                pooledObject.SetActive(true);
                return pooledObject;
            }

            GameObject created = CreateInstance(prefab);
            created.SetActive(true);
            return created;
        }

        public void ReturnVehicle(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!prefabByInstance.TryGetValue(instance, out GameObject prefab))
            {
                Destroy(instance);
                return;
            }

            if (!pooledByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pooledByPrefab[prefab] = queue;
            }

            instance.SetActive(false);
            if (pooledVehiclesParent != null)
            {
                instance.transform.SetParent(pooledVehiclesParent);
            }

            queue.Enqueue(instance);
        }

        private GameObject CreateInstance(GameObject prefab)
        {
            Transform parent = pooledVehiclesParent != null ? pooledVehiclesParent : transform;
            GameObject instance = Instantiate(prefab, parent);
            instance.SetActive(false);
            prefabByInstance[instance] = prefab;
            return instance;
        }
    }
}
