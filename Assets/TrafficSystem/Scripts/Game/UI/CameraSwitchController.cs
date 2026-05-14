using MyTrafficSystem.Gameplay.CCTV;
using UnityEngine;

namespace MyTrafficSystem.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class CameraSwitchController : MonoBehaviour
    {
        [SerializeField] private CCTVCameraSystem cctvCameraSystem;

        private void Awake()
        {
            if (cctvCameraSystem == null) cctvCameraSystem = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
        }

        public void SwitchToCameraIndex(int index)
        {
            if (cctvCameraSystem == null) return;
            cctvCameraSystem.SetActiveCamera(index);
        }

        public void NextCamera()
        {
            if (cctvCameraSystem == null) return;
            cctvCameraSystem.NextCamera();
        }

        public void PreviousCamera()
        {
            if (cctvCameraSystem == null) return;
            cctvCameraSystem.PreviousCamera();
        }
    }
}
