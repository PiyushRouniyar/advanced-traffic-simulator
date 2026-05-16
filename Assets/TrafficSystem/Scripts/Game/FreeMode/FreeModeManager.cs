using MyTrafficSystem.Gameplay.CCTV;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.FreeMode
{
    [DisallowMultipleComponent]
    public class FreeModeManager : MonoBehaviour
    {
        [SerializeField] private CCTVCameraSystem cctvSystem;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private GameObject playerVehiclePrefab;
        [SerializeField] private FreeModeSpawnPoint[] spawnPoints;
        [SerializeField] private bool autoFindSpawnPoints = true;

        private GameObject activeVehicle;
        private FreeModeCameraFollow followRig;
        private bool active;
        private bool hasLoggedReady;

        public bool IsFreeModeActive => active;
        public bool IsReady { get; private set; }
        public float CurrentSpeedKph
        {
            get
            {
                if (activeVehicle == null) return 0f;
                FreeModeVehicleController controller = activeVehicle.GetComponent<FreeModeVehicleController>();
                if (controller != null) return controller.SpeedMps * 3.6f;
                Rigidbody rb = activeVehicle.GetComponent<Rigidbody>();
                return rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
            }
        }

        private void Awake()
        {
            EnsureReady(log: false);
        }

        public bool EnsureReady(bool log = true)
        {
            if (cctvSystem == null) cctvSystem = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (autoFindSpawnPoints || spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = FindObjectsByType<FreeModeSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }

            IsReady = gameplayCamera != null && spawnPoints != null && spawnPoints.Length > 0;
            if (log)
            {
                if (IsReady && !hasLoggedReady)
                {
                    Debug.Log($"[OK] {nameof(FreeModeManager)} ready. Spawns: {spawnPoints.Length}");
                    hasLoggedReady = true;
                }
                else if (!IsReady)
                {
                    Debug.LogWarning($"[WARN] {nameof(FreeModeManager)} not ready. Camera={gameplayCamera != null}, SpawnPoints={(spawnPoints != null ? spawnPoints.Length : 0)}");
                }
            }

            return IsReady;
        }

        public void EnterFreeMode()
        {
            if (!EnsureReady())
            {
                return;
            }

            FreeModeSpawnPoint spawn = ChooseSpawnPoint();
            if (spawn == null || gameplayCamera == null) return;

            if (activeVehicle == null)
            {
                activeVehicle = SpawnVehicle(spawn.transform.position, spawn.transform.rotation);
            }
            else
            {
                Rigidbody rb = activeVehicle.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
                activeVehicle.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
                activeVehicle.SetActive(true);
            }

            followRig = gameplayCamera.GetComponent<FreeModeCameraFollow>();
            if (followRig == null) followRig = gameplayCamera.gameObject.AddComponent<FreeModeCameraFollow>();
            followRig.SetTarget(activeVehicle.transform);
            followRig.enabled = true;

            if (cctvSystem != null)
            {
                cctvSystem.SetExternalCameraControl(true);
                cctvSystem.SetCameraSelectionLocked(false);
            }

            active = true;
        }

        public void ExitFreeMode()
        {
            if (!active) return;
            active = false;

            if (followRig != null)
            {
                followRig.SetTarget(null);
                followRig.enabled = false;
            }

            if (activeVehicle != null)
            {
                Rigidbody rb = activeVehicle.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }

            if (cctvSystem != null)
            {
                cctvSystem.SetExternalCameraControl(false);
            }
        }

        private FreeModeSpawnPoint ChooseSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return null;

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null && spawnPoints[i].Preferred && IsSpawnSafe(spawnPoints[i]))
                {
                    return spawnPoints[i];
                }
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null && IsSpawnSafe(spawnPoints[i]))
                {
                    return spawnPoints[i];
                }
            }

            return spawnPoints[0];
        }

        private static bool IsSpawnSafe(FreeModeSpawnPoint p)
        {
            Collider[] hits = Physics.OverlapSphere(p.transform.position, p.ClearanceRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;
                if (hits[i].GetComponentInParent<MyTrafficSystem.AI.TrafficCarAI>() != null) return false;
            }
            return true;
        }

        private GameObject SpawnVehicle(Vector3 position, Quaternion rotation)
        {
            GameObject vehicle = playerVehiclePrefab != null
                ? Instantiate(playerVehiclePrefab, position, rotation)
                : CreateFallbackVehicle(position, rotation);

            if (vehicle.GetComponent<FreeModeVehicleController>() == null)
            {
                vehicle.AddComponent<FreeModeVehicleController>();
            }

            if (vehicle.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = vehicle.AddComponent<Rigidbody>();
                rb.mass = 1200f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            return vehicle;
        }

        private static GameObject CreateFallbackVehicle(Vector3 position, Quaternion rotation)
        {
            GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "FreeModeCar";
            car.transform.SetPositionAndRotation(position, rotation);
            car.transform.localScale = new Vector3(1.6f, 0.65f, 3.4f);
            Renderer r = car.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.16f, 0.28f, 0.38f, 1f);
            return car;
        }
    }
}
