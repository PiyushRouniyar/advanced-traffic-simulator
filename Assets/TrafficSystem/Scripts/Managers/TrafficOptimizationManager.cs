using System.Collections.Generic;
using MyTrafficSystem.Vehicles;
using UnityEngine;

namespace MyTrafficSystem.Managers
{
    /// <summary>
    /// Central manager for distance-based traffic optimization.
    /// </summary>
    public class TrafficOptimizationManager : MonoBehaviour
    {
        public static TrafficOptimizationManager Instance { get; private set; }

        [Header("Viewer")]
        [SerializeField] private Transform viewer;

        [Header("Update Loop")]
        [SerializeField] private float updateInterval = 0.2f;
        [SerializeField] private int vehiclesPerBatch = 25;
        [SerializeField] private bool autoFindVehicles = true;

        [Header("Debug")]
        [SerializeField] private bool logVehicleCountOnStart = true;

        private readonly List<VehicleLOD> vehicleLods = new List<VehicleLOD>();
        private float updateTimer;
        private int nextIndex;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {

            if (viewer == null && Camera.main != null)
            {
                viewer = Camera.main.transform;
            }

            if (autoFindVehicles)
            {
                RefreshVehicleList();
            }

            if (logVehicleCountOnStart)
            {
                Debug.Log($"TrafficOptimizationManager tracking {vehicleLods.Count} vehicles.");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterVehicle(VehicleLOD vehicleLod)
        {
            if (vehicleLod == null || vehicleLods.Contains(vehicleLod))
            {
                return;
            }

            vehicleLods.Add(vehicleLod);
        }

        public void UnregisterVehicle(VehicleLOD vehicleLod)
        {
            if (vehicleLod == null)
            {
                return;
            }

            vehicleLods.Remove(vehicleLod);
        }

        private void Update()
        {
            if (viewer == null)
            {
                return;
            }

            updateTimer -= Time.deltaTime;
            if (updateTimer > 0f)
            {
                return;
            }

            updateTimer = Mathf.Max(0.05f, updateInterval);
            ProcessBatch();
        }

        [ContextMenu("Refresh Vehicle LOD List")]
        public void RefreshVehicleList()
        {
            vehicleLods.Clear();
            VehicleLOD[] found = FindObjectsOfType<VehicleLOD>(true);
            for (int i = 0; i < found.Length; i++)
            {
                vehicleLods.Add(found[i]);
            }

            nextIndex = 0;
        }

        private void ProcessBatch()
        {
            if (vehicleLods.Count == 0)
            {
                return;
            }

            int batchCount = Mathf.Max(1, vehiclesPerBatch);
            int processed = 0;

            while (processed < batchCount && vehicleLods.Count > 0)
            {
                if (nextIndex >= vehicleLods.Count)
                {
                    nextIndex = 0;
                }

                VehicleLOD lod = vehicleLods[nextIndex];
                nextIndex++;
                processed++;

                if (lod == null || !lod.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector3.Distance(viewer.position, lod.transform.position);
                lod.ApplyDistanceLOD(distance);
            }
        }
    }
}
