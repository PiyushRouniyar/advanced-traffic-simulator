using System.Collections.Generic;
using System;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.CCTV
{
    [DisallowMultipleComponent]
    public class CCTVCameraSystem : MonoBehaviour
    {
        public enum SwitchMode
        {
            HardCut,
            FastBlend
        }

        [Header("Runtime Camera")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private SwitchMode switchMode = SwitchMode.HardCut;
        [SerializeField] private float switchBlendTime = 0.12f;
        [SerializeField] private KeyCode nextCameraKey = KeyCode.Tab;
        [SerializeField] private KeyCode zoomKey = KeyCode.Z;
        [SerializeField] private bool autoDiscoverCameraPointsIfNone = true;
        [SerializeField] private bool sortByPriority = true;
        [SerializeField] private bool enforceFixedMountEveryFrame = true;
        [SerializeField] private bool allowKeyboardSwitching = true;
        [SerializeField] private KeyCode freeRoamToggleKey = KeyCode.F;
        [SerializeField] private float freeRoamMoveSpeed = 16f;
        [SerializeField] private float freeRoamLookSpeed = 90f;
        [SerializeField] private bool holdRightMouseToLook = true;

        private readonly List<Transform> cameraAnchors = new List<Transform>();
        private int activeCameraIndex = -1;
        private Transform blendTarget;
        private float currentBlendTime;
        private bool cameraSelectionLocked;
        private bool freeRoamEnabled;
        private float freeRoamYaw;
        private float freeRoamPitch;

        public int ActiveCameraIndex => activeCameraIndex;
        public int CameraCount => cameraAnchors.Count;
        public CCTVCameraPoint ActivePoint => (activeCameraIndex >= 0 && activeCameraIndex < cameraAnchors.Count && cameraAnchors[activeCameraIndex] != null)
            ? cameraAnchors[activeCameraIndex].GetComponent<CCTVCameraPoint>()
            : null;
        public string ActiveCameraLabel
        {
            get
            {
                if (activeCameraIndex < 0 || activeCameraIndex >= cameraAnchors.Count) return "N/A";
                CCTVCameraPoint p = cameraAnchors[activeCameraIndex] != null ? cameraAnchors[activeCameraIndex].GetComponent<CCTVCameraPoint>() : null;
                return p != null ? p.CameraLabel : cameraAnchors[activeCameraIndex].name;
            }
        }
        public string ActiveCameraObjectName
        {
            get
            {
                if (activeCameraIndex < 0 || activeCameraIndex >= cameraAnchors.Count) return "N/A";
                return cameraAnchors[activeCameraIndex] != null ? cameraAnchors[activeCameraIndex].name : "N/A";
            }
        }
        public string ActiveIntersectionName => ActivePoint != null ? ActivePoint.IntersectionLabel : "No Intersection";
        public string ActiveTrafficGroupName => ActivePoint != null ? ActivePoint.TrafficGroupLabel : "No Group";
        public bool CameraSelectionLocked => cameraSelectionLocked;
        public bool FreeRoamEnabled => freeRoamEnabled;

        public string GetCameraLabel(int index)
        {
            if (index < 0 || index >= cameraAnchors.Count || cameraAnchors[index] == null) return $"CAM {index + 1:00}";
            CCTVCameraPoint p = cameraAnchors[index].GetComponent<CCTVCameraPoint>();
            return p != null ? p.CameraLabel : cameraAnchors[index].name;
        }

        public event Action<CCTVCameraPoint, int> CameraChanged;

        private void Awake()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (cameraAnchors.Count == 0 && autoDiscoverCameraPointsIfNone)
            {
                DiscoverCameraPoints();
            }
            if (gameplayCamera != null)
            {
                Vector3 e = gameplayCamera.transform.eulerAngles;
                freeRoamYaw = e.y;
                freeRoamPitch = e.x;
            }
        }

        private void Update()
        {
            if (cameraAnchors.Count == 0 || gameplayCamera == null) return;

            if (Input.GetKeyDown(freeRoamToggleKey))
            {
                SetFreeRoamEnabled(!freeRoamEnabled);
            }

            if (freeRoamEnabled)
            {
                UpdateFreeRoamCamera();
                return;
            }

            if (allowKeyboardSwitching && !cameraSelectionLocked)
            {
                if (Input.GetKeyDown(nextCameraKey))
                {
                    NextCamera();
                }

                if (Input.GetKeyDown(zoomKey))
                {
                    ToggleZoom();
                }
            }

            if (blendTarget != null)
            {
                if (switchMode == SwitchMode.HardCut)
                {
                    gameplayCamera.transform.SetPositionAndRotation(blendTarget.position, blendTarget.rotation);
                }
                else
                {
                    float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.01f, switchBlendTime));
                    currentBlendTime += Time.deltaTime;
                    gameplayCamera.transform.position = Vector3.Lerp(gameplayCamera.transform.position, blendTarget.position, Mathf.SmoothStep(0f, 1f, t * 6f));
                    gameplayCamera.transform.rotation = Quaternion.Slerp(gameplayCamera.transform.rotation, blendTarget.rotation, Mathf.SmoothStep(0f, 1f, t * 6f));
                }
            }
        }

        private void LateUpdate()
        {
            if (freeRoamEnabled) return;
            if (!enforceFixedMountEveryFrame || gameplayCamera == null || blendTarget == null) return;
            if (switchMode == SwitchMode.HardCut)
            {
                gameplayCamera.transform.SetPositionAndRotation(blendTarget.position, blendTarget.rotation);
            }
        }

        public void SetCameraAnchors(IEnumerable<Transform> anchors)
        {
            cameraAnchors.Clear();
            if (anchors == null) return;
            foreach (Transform t in anchors)
            {
                if (t != null) cameraAnchors.Add(t);
            }

            if (sortByPriority)
            {
                cameraAnchors.Sort((a, b) =>
                {
                    CCTVCameraPoint pa = a != null ? a.GetComponent<CCTVCameraPoint>() : null;
                    CCTVCameraPoint pb = b != null ? b.GetComponent<CCTVCameraPoint>() : null;
                    float av = pa != null ? pa.Priority : 0f;
                    float bv = pb != null ? pb.Priority : 0f;
                    return bv.CompareTo(av);
                });
            }

            if (cameraAnchors.Count > 0)
            {
                SetActiveCamera(0, instant: true);
            }
        }

        public void DiscoverCameraPoints()
        {
            CCTVCameraPoint[] points = FindObjectsByType<CCTVCameraPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            List<Transform> anchors = new List<Transform>(points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null) anchors.Add(points[i].transform);
            }
            SetCameraAnchors(anchors);
        }

        public void SetActiveCamera(int index, bool instant = false)
        {
            if (cameraSelectionLocked && index != activeCameraIndex) return;
            if (cameraAnchors.Count == 0 || index < 0 || index >= cameraAnchors.Count) return;

            activeCameraIndex = index;
            blendTarget = cameraAnchors[index];
            currentBlendTime = 0f;
            if (gameplayCamera == null || blendTarget == null) return;

            CCTVCameraPoint point = blendTarget.GetComponent<CCTVCameraPoint>();
            if (point != null)
            {
                gameplayCamera.fieldOfView = point.FieldOfView;
            }

            if (instant || switchMode == SwitchMode.HardCut)
            {
                gameplayCamera.transform.SetPositionAndRotation(blendTarget.position, blendTarget.rotation);
            }

            CameraChanged?.Invoke(point, activeCameraIndex);
        }

        public void NextCamera()
        {
            if (cameraSelectionLocked) return;
            if (cameraAnchors.Count == 0) return;
            int next = (activeCameraIndex + 1 + cameraAnchors.Count) % cameraAnchors.Count;
            SetActiveCamera(next);
        }

        public void PreviousCamera()
        {
            if (cameraSelectionLocked) return;
            if (cameraAnchors.Count == 0) return;
            int prev = (activeCameraIndex - 1 + cameraAnchors.Count) % cameraAnchors.Count;
            SetActiveCamera(prev);
        }

        public void SetCameraSelectionLocked(bool locked)
        {
            cameraSelectionLocked = locked;
        }

        public void SetFreeRoamEnabled(bool enabled)
        {
            freeRoamEnabled = enabled;
            if (gameplayCamera == null) return;

            if (enabled)
            {
                blendTarget = null;
                Vector3 e = gameplayCamera.transform.eulerAngles;
                freeRoamYaw = e.y;
                freeRoamPitch = e.x;
                cameraSelectionLocked = false;
                return;
            }

            if (activeCameraIndex >= 0 && activeCameraIndex < cameraAnchors.Count)
            {
                SetActiveCamera(activeCameraIndex, instant: true);
            }
        }

        private void UpdateFreeRoamCamera()
        {
            float dt = Time.deltaTime;
            float speed = freeRoamMoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                speed *= 2.2f;
            }

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            float y = 0f;
            if (Input.GetKey(KeyCode.E)) y += 1f;
            if (Input.GetKey(KeyCode.Q)) y -= 1f;

            Vector3 move = gameplayCamera.transform.right * x + gameplayCamera.transform.forward * z + Vector3.up * y;
            gameplayCamera.transform.position += move * (speed * dt);

            bool canLook = !holdRightMouseToLook || Input.GetMouseButton(1);
            if (!canLook) return;

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            freeRoamYaw += mouseX * freeRoamLookSpeed * dt;
            freeRoamPitch -= mouseY * freeRoamLookSpeed * dt;
            freeRoamPitch = Mathf.Clamp(freeRoamPitch, -80f, 80f);
            gameplayCamera.transform.rotation = Quaternion.Euler(freeRoamPitch, freeRoamYaw, 0f);
        }

        private void ToggleZoom()
        {
            CCTVCameraPoint point = ActivePoint;
            if (point == null || gameplayCamera == null || !point.AllowZoom) return;
            float normalFov = point.FieldOfView;
            float zoomFov = point.ZoomFieldOfView;
            bool zoomed = Mathf.Abs(gameplayCamera.fieldOfView - zoomFov) < 0.5f;
            gameplayCamera.fieldOfView = zoomed ? normalFov : zoomFov;
        }
    }
}
