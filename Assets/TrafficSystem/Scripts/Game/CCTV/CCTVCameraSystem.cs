using System.Collections.Generic;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.CCTV
{
    [DisallowMultipleComponent]
    public class CCTVCameraSystem : MonoBehaviour
    {
        [Header("Runtime Camera")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private float switchBlendTime = 0.5f;
        [SerializeField] private KeyCode nextCameraKey = KeyCode.Tab;
        [SerializeField] private KeyCode previousCameraKey = KeyCode.BackQuote;

        private readonly List<Transform> cameraAnchors = new List<Transform>();
        private int activeCameraIndex = -1;
        private float blendVelocity;
        private Transform blendTarget;

        public int ActiveCameraIndex => activeCameraIndex;
        public int CameraCount => cameraAnchors.Count;
        public string ActiveCameraLabel
        {
            get
            {
                if (activeCameraIndex < 0 || activeCameraIndex >= cameraAnchors.Count) return "N/A";
                CCTVCameraPoint p = cameraAnchors[activeCameraIndex] != null ? cameraAnchors[activeCameraIndex].GetComponent<CCTVCameraPoint>() : null;
                return p != null ? p.CameraLabel : cameraAnchors[activeCameraIndex].name;
            }
        }

        private void Awake()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        private void Update()
        {
            if (cameraAnchors.Count == 0 || gameplayCamera == null) return;

            if (Input.GetKeyDown(nextCameraKey)) NextCamera();
            if (Input.GetKeyDown(previousCameraKey)) PreviousCamera();
            HandleNumericShortcuts();

            if (blendTarget != null)
            {
                float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.01f, switchBlendTime));
                gameplayCamera.transform.position = Vector3.Lerp(gameplayCamera.transform.position, blendTarget.position, t * 3f);
                gameplayCamera.transform.rotation = Quaternion.Slerp(gameplayCamera.transform.rotation, blendTarget.rotation, t * 3f);
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

            if (cameraAnchors.Count > 0)
            {
                SetActiveCamera(0, instant: true);
            }
        }

        public void SetActiveCamera(int index, bool instant = false)
        {
            if (cameraAnchors.Count == 0 || index < 0 || index >= cameraAnchors.Count) return;

            activeCameraIndex = index;
            blendTarget = cameraAnchors[index];
            if (gameplayCamera == null || blendTarget == null) return;

            CCTVCameraPoint point = blendTarget.GetComponent<CCTVCameraPoint>();
            if (point != null)
            {
                gameplayCamera.fieldOfView = point.FieldOfView;
            }

            if (instant)
            {
                gameplayCamera.transform.SetPositionAndRotation(blendTarget.position, blendTarget.rotation);
            }
        }

        public void NextCamera()
        {
            if (cameraAnchors.Count == 0) return;
            int next = (activeCameraIndex + 1 + cameraAnchors.Count) % cameraAnchors.Count;
            SetActiveCamera(next);
        }

        public void PreviousCamera()
        {
            if (cameraAnchors.Count == 0) return;
            int prev = (activeCameraIndex - 1 + cameraAnchors.Count) % cameraAnchors.Count;
            SetActiveCamera(prev);
        }

        private void HandleNumericShortcuts()
        {
            int max = Mathf.Min(9, cameraAnchors.Count);
            for (int i = 0; i < max; i++)
            {
                KeyCode key = KeyCode.Alpha1 + i;
                if (Input.GetKeyDown(key))
                {
                    SetActiveCamera(i);
                }
            }
        }
    }
}
