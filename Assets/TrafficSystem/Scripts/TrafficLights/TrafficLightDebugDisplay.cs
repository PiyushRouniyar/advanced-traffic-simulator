using UnityEngine;

namespace MyTrafficSystem.TrafficLights
{
    [DisallowMultipleComponent]
    public class TrafficLightDebugDisplay : MonoBehaviour
    {
        [SerializeField] private float rescanInterval = 1.2f;

        private float timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<TrafficLightDebugDisplay>() != null)
            {
                return;
            }

            GameObject go = new GameObject("TrafficLightDebugDisplay");
            DontDestroyOnLoad(go);
            go.AddComponent<TrafficLightDebugDisplay>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(TrafficLightDebugSettings.ToggleKey))
            {
                TrafficLightDebugSettings.ShowTrafficLightDebugInfo = !TrafficLightDebugSettings.ShowTrafficLightDebugInfo;
            }

            timer -= Time.unscaledDeltaTime;
            if (timer > 0f)
            {
                return;
            }

            timer = Mathf.Max(0.2f, rescanInterval);
            EnsureLabelsForAllLights();
        }

        private static void EnsureLabelsForAllLights()
        {
            TrafficLightController[] lights = FindObjectsByType<TrafficLightController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null)
                {
                    continue;
                }

                if (lights[i].GetComponent<TrafficLightWorldLabel>() == null)
                {
                    lights[i].gameObject.AddComponent<TrafficLightWorldLabel>();
                }
            }
        }
    }
}
