using UnityEngine;

namespace MyTrafficSystem.Gameplay.UI
{
    [DisallowMultipleComponent]
    public class LevelMenuController : MonoBehaviour
    {
        [SerializeField] private MyTrafficSystem.Gameplay.TrafficGameManager trafficGameManager;

        private void Awake()
        {
            if (trafficGameManager == null) trafficGameManager = FindFirstObjectByType<MyTrafficSystem.Gameplay.TrafficGameManager>(FindObjectsInactive.Include);
        }

        public void StartLevel(int levelIndex)
        {
            if (trafficGameManager == null) return;
            Time.timeScale = 1f;
            trafficGameManager.StartLevel(levelIndex);
        }

        public void Retry()
        {
            if (trafficGameManager == null) return;
            Time.timeScale = 1f;
            trafficGameManager.RetryLevel();
        }

        public void NextLevel()
        {
            if (trafficGameManager == null) return;
            Time.timeScale = 1f;
            trafficGameManager.NextLevel();
        }

        public void Resume()
        {
            if (trafficGameManager == null) return;
            trafficGameManager.SetPaused(false);
        }

        public void Pause()
        {
            if (trafficGameManager == null) return;
            trafficGameManager.SetPaused(true);
        }
    }
}
