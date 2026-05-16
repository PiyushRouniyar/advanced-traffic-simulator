using System.Collections;
using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.Gameplay.Challenge;
using MyTrafficSystem.Gameplay.FreeMode;
using MyTrafficSystem.Gameplay.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace MyTrafficSystem.Gameplay
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class GameplayInitializationManager : MonoBehaviour
    {
        private static GameplayInitializationManager instance;

        [Header("Startup")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private float readinessTimeoutSeconds = 8f;
        [SerializeField] private bool verboseLogs = true;

        private bool booting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<GameplayInitializationManager>(FindObjectsInactive.Include) != null) return;
            GameObject go = new GameObject("GameplayInitializationManager");
            go.AddComponent<GameplayInitializationManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            TryBootScene(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootScene(scene);
        }

        private void TryBootScene(Scene scene)
        {
            if (booting || !scene.IsValid() || !scene.isLoaded) return;
            if (IsMainMenuScene(scene.name)) return;
            StartCoroutine(BootstrapRoutine(scene));
        }

        private IEnumerator BootstrapRoutine(Scene scene)
        {
            booting = true;

            Log($"[INIT] Scene loaded: {scene.name}");
            EnsureEventSystem();
            yield return null;

            CCTVCameraSystem cctv = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            if (cctv != null && cctv.CameraCount == 0)
            {
                cctv.DiscoverCameraPoints();
            }
            Log(cctv != null ? "[OK] CCTVSystem initialized" : "[WARN] CCTVSystem missing");

            FreeModeManager freeMode = FindFirstObjectByType<FreeModeManager>(FindObjectsInactive.Include);
            if (freeMode != null)
            {
                freeMode.EnsureReady(verboseLogs);
                Log("[OK] Free Mode ready");
            }
            else
            {
                Log("[WARN] FreeModeManager missing");
            }

            CameraChallengeManager challenge = FindFirstObjectByType<CameraChallengeManager>(FindObjectsInactive.Include);
            if (challenge == null)
            {
                GameObject challengeGo = new GameObject("CameraChallengeManager");
                challenge = challengeGo.AddComponent<CameraChallengeManager>();
            }
            challenge.EnsureReady(verboseLogs);
            Log("[OK] Monitor Mode ready");

            TrafficGameManager gameplay = FindFirstObjectByType<TrafficGameManager>(FindObjectsInactive.Include);
            if (gameplay != null)
            {
                gameplay.EnsureReferences(verboseLogs);
                Log("[OK] TrafficGameManager initialized");
            }
            else
            {
                Log("[WARN] TrafficGameManager not found in scene (level flow disabled, CCTV/free-mode still available)");
            }

            float timer = readinessTimeoutSeconds;
            while (timer > 0f)
            {
                bool cctvReady = cctv != null && cctv.CameraCount > 0;
                bool challengeReady = challenge != null && challenge.EnsureReady(log: false);
                bool freeReady = freeMode == null || freeMode.EnsureReady(log: false);
                bool gameReady = gameplay == null || gameplay.EnsureReferences(log: false);
                bool allReady = cctvReady && challengeReady && freeReady && gameReady;

                if (allReady)
                {
                    Log("[OK] Gameplay readiness check passed");
                    break;
                }

                timer -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (timer <= 0f)
            {
                Log("[WARN] Gameplay readiness timeout; systems will keep retrying during runtime");
            }

            booting = false;
        }

        private static bool IsMainMenuScene(string sceneName)
        {
            return sceneName.ToLowerInvariant().Contains("menu");
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                _ = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                return;
            }

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
        }

        private void Log(string message)
        {
            if (!verboseLogs && message.StartsWith("[OK]")) return;
            Debug.Log(message);
        }
    }
}
