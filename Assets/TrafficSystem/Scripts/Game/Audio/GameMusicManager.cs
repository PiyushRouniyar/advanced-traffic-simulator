using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyTrafficSystem.Gameplay.Audio
{
    [DisallowMultipleComponent]
    public class GameMusicManager : MonoBehaviour
    {
        [Header("Tracks")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;

        [Header("Scenes")]
        [SerializeField] private string menuSceneName = "MainMenu";

        [Header("Mix")]
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.65f;
        [SerializeField] private float crossfadeDuration = 1.2f;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private Coroutine transitionRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<GameMusicManager>(FindObjectsInactive.Include) != null) return;
            GameObject go = new GameObject("GameMusicManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameMusicManager>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            sourceA = CreateSource("MusicA");
            sourceB = CreateSource("MusicB");
            activeSource = sourceA;
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
            UpdateTrackForScene(SceneManager.GetActiveScene().name, immediate: true);
        }

        public void SetMusicVolume(float volume01)
        {
            musicVolume = Mathf.Clamp01(volume01);
            if (activeSource != null) activeSource.volume = musicVolume;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateTrackForScene(scene.name, immediate: false);
        }

        private void UpdateTrackForScene(string sceneName, bool immediate)
        {
            bool isMenu = string.Equals(sceneName, menuSceneName, System.StringComparison.OrdinalIgnoreCase);
            AudioClip desired = isMenu ? menuMusic : gameplayMusic;
            if (desired == null) return;
            if (activeSource != null && activeSource.clip == desired && activeSource.isPlaying) return;

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(SwitchTrackRoutine(desired, immediate));
        }

        private IEnumerator SwitchTrackRoutine(AudioClip nextClip, bool immediate)
        {
            AudioSource from = activeSource;
            AudioSource to = from == sourceA ? sourceB : sourceA;
            to.clip = nextClip;
            to.volume = immediate ? musicVolume : 0f;
            to.loop = true;
            to.Play();

            if (!immediate)
            {
                float duration = Mathf.Max(0.05f, crossfadeDuration);
                float t = 0f;
                float fromStart = from != null ? from.volume : 0f;
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / duration);
                    if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                    to.volume = Mathf.Lerp(0f, musicVolume, k);
                    yield return null;
                }
            }

            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
            }

            to.volume = musicVolume;
            activeSource = to;
            transitionRoutine = null;
        }

        private AudioSource CreateSource(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(transform, false);
            AudioSource src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.volume = 0f;
            return src;
        }
    }
}
