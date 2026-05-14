using UnityEngine;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    public class MasterTrafficLightController : MonoBehaviour
    {
        private static MasterTrafficLightController instance;

        [Header("Optional Hotkeys")]
        [SerializeField] private bool enableKeyboardShortcuts;
        [SerializeField] private KeyCode setAllGreenKey = KeyCode.F6;
        [SerializeField] private KeyCode setAllRedKey = KeyCode.F7;
        [SerializeField] private KeyCode toggleAllKey = KeyCode.F8;

        [Header("State")]
        [SerializeField] private bool lastCommandWasGreen;

        private MyTrafficSystem.TrafficLights.TrafficLightController[] lights = System.Array.Empty<MyTrafficSystem.TrafficLights.TrafficLightController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstanceExists()
        {
            if (FindFirstObjectByType<MasterTrafficLightController>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject go = new GameObject("MasterTrafficLightController");
            go.AddComponent<MasterTrafficLightController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            RefreshLights();
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts) return;

            if (Input.GetKeyDown(setAllGreenKey))
            {
                SetAllGreen();
            }
            else if (Input.GetKeyDown(setAllRedKey))
            {
                SetAllRed();
            }
            else if (Input.GetKeyDown(toggleAllKey))
            {
                ToggleAll();
            }
        }

        public void RefreshLights()
        {
            lights = FindObjectsByType<MyTrafficSystem.TrafficLights.TrafficLightController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void SetAllGreen()
        {
            ApplyToAll(true);
            lastCommandWasGreen = true;
        }

        public void SetAllRed()
        {
            ApplyToAll(false);
            lastCommandWasGreen = false;
        }

        public void ToggleAll()
        {
            if (lastCommandWasGreen) SetAllRed();
            else SetAllGreen();
        }

        private void ApplyToAll(bool setGreen)
        {
            if (lights == null || lights.Length == 0)
            {
                RefreshLights();
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                if (setGreen) lights[i].SetGreen();
                else lights[i].SetRed();
            }
        }
    }
}
