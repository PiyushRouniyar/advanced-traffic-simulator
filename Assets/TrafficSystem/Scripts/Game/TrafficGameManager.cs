using System;
using System.Collections.Generic;
using MyTrafficSystem.Gameplay.CCTV;
using MyTrafficSystem.Gameplay.Level;
using MyTrafficSystem.Gameplay.Systems;
using MyTrafficSystem.Gameplay.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyTrafficSystem.Gameplay
{
    [DisallowMultipleComponent]
    public class TrafficGameManager : MonoBehaviour
    {
        [Header("Level Content")]
        [SerializeField] private List<TrafficLevelDefinition> levels = new List<TrafficLevelDefinition>();
        [SerializeField] private int startLevelIndex;

        [Header("Systems")]
        [SerializeField] private CCTVCameraSystem cctvCameraSystem;
        [SerializeField] private TrafficCongestionMonitor congestionMonitor;
        [SerializeField] private TrafficPressureSystem pressureSystem;
        [SerializeField] private TrafficObjectiveSystem objectiveSystem;
        [SerializeField] private TrafficScoreSystem scoreSystem;
        [SerializeField] private TrafficGameHUD hud;

        [Header("Gameplay")]
        [SerializeField] private bool autoStartFirstLevel = true;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private TrafficGameState state = TrafficGameState.MainMenu;
        private TrafficLevelDefinition activeLevel;
        private int activeLevelIndex = -1;
        private float introTimer;
        private float elapsedLevel;

        public TrafficGameState State => state;

        private void Awake()
        {
            if (cctvCameraSystem == null) cctvCameraSystem = FindFirstObjectByType<CCTVCameraSystem>(FindObjectsInactive.Include);
            if (congestionMonitor == null) congestionMonitor = FindFirstObjectByType<TrafficCongestionMonitor>(FindObjectsInactive.Include);
            if (pressureSystem == null) pressureSystem = FindFirstObjectByType<TrafficPressureSystem>(FindObjectsInactive.Include);
            if (objectiveSystem == null) objectiveSystem = FindFirstObjectByType<TrafficObjectiveSystem>(FindObjectsInactive.Include);
            if (scoreSystem == null) scoreSystem = FindFirstObjectByType<TrafficScoreSystem>(FindObjectsInactive.Include);
            if (hud == null) hud = FindFirstObjectByType<TrafficGameHUD>(FindObjectsInactive.Include);

            if (objectiveSystem != null)
            {
                objectiveSystem.ObjectiveFailed += OnObjectiveFailed;
            }
        }

        private void Start()
        {
            SetState(TrafficGameState.MainMenu);
            if (autoStartFirstLevel)
            {
                StartLevel(Mathf.Clamp(startLevelIndex, 0, levels.Count - 1));
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                if (state == TrafficGameState.Playing) SetPaused(true);
                else if (state == TrafficGameState.Paused) SetPaused(false);
            }

            if (state == TrafficGameState.LevelIntro)
            {
                introTimer -= Time.deltaTime;
                if (introTimer <= 0f)
                {
                    BeginGameplay();
                }
                return;
            }

            if (state != TrafficGameState.Playing || activeLevel == null) return;

            elapsedLevel += Time.deltaTime;
            float remaining = Mathf.Max(0f, activeLevel.LevelDurationSeconds - elapsedLevel);

            congestionMonitor?.Evaluate();
            objectiveSystem?.Tick(Time.deltaTime);
            pressureSystem?.Tick(elapsedLevel);
            scoreSystem?.Tick(Time.deltaTime);

            UpdateHud(remaining);

            if (elapsedLevel >= activeLevel.LevelDurationSeconds)
            {
                CompleteLevelWin();
            }
        }

        public void StartLevel(int index)
        {
            if (index < 0 || index >= levels.Count) return;

            activeLevelIndex = index;
            activeLevel = levels[index];
            elapsedLevel = 0f;
            introTimer = activeLevel.IntroDurationSeconds;

            pressureSystem?.SetLevel(activeLevel);
            objectiveSystem?.Configure(activeLevel, congestionMonitor);
            scoreSystem?.ResetForLevel(congestionMonitor);
            cctvCameraSystem?.SetCameraAnchors(activeLevel.CctvCameraAnchors);

            if (hud != null)
            {
                hud.SetLevelName(activeLevel.DisplayName);
                hud.ShowIntro(activeLevel.IntroText);
                hud.HideResult();
            }

            SetState(TrafficGameState.LevelIntro);
        }

        public void RetryLevel()
        {
            if (activeLevel == null)
            {
                StartLevel(Mathf.Clamp(startLevelIndex, 0, levels.Count - 1));
                return;
            }

            string scene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(scene);
        }

        public void NextLevel()
        {
            int next = activeLevelIndex + 1;
            if (next >= levels.Count)
            {
                SetState(TrafficGameState.MainMenu);
                return;
            }
            StartLevel(next);
        }

        public void SetPaused(bool paused)
        {
            if (paused && state == TrafficGameState.Playing)
            {
                SetState(TrafficGameState.Paused);
                Time.timeScale = 0f;
            }
            else if (!paused && state == TrafficGameState.Paused)
            {
                Time.timeScale = 1f;
                SetState(TrafficGameState.Playing);
            }
        }

        private void BeginGameplay()
        {
            hud?.HideIntro();
            Time.timeScale = 1f;
            SetState(TrafficGameState.Playing);
        }

        private void CompleteLevelWin()
        {
            SetState(TrafficGameState.Win);
            Time.timeScale = 0f;
            int stars = scoreSystem != null ? scoreSystem.CalculateStars(activeLevel) : 1;
            string starString = new string('?', Mathf.Clamp(stars, 1, 3));
            hud?.ShowResult($"LEVEL CLEAR\n{starString}\nScore: {scoreSystem?.CurrentScore ?? 0}");
        }

        private void OnObjectiveFailed(string reason)
        {
            if (state != TrafficGameState.Playing) return;
            SetState(TrafficGameState.Lose);
            Time.timeScale = 0f;
            hud?.ShowResult($"LEVEL FAILED\n{reason}\nScore: {scoreSystem?.CurrentScore ?? 0}");
        }

        private void UpdateHud(float remaining)
        {
            if (hud == null) return;

            hud.SetTimer(remaining);
            hud.SetGameState(state.ToString());
            hud.SetCamera(cctvCameraSystem != null ? cctvCameraSystem.ActiveCameraLabel : "N/A");

            if (congestionMonitor != null)
            {
                hud.SetCongestion(congestionMonitor.NormalizedCongestion, congestionMonitor.StalledVehicleCount, congestionMonitor.ActiveVehicleCount);
                hud.SetFlow(1f - congestionMonitor.NormalizedCongestion);
                hud.SetPedestrianStatus(congestionMonitor.WaitingCitizenCount);
            }

            if (scoreSystem != null)
            {
                hud.SetScore(scoreSystem.CurrentScore);
            }

            if (pressureSystem != null)
            {
                hud.SetPressure(pressureSystem.Pressure01);
            }
        }

        private void SetState(TrafficGameState next)
        {
            state = next;
            if (hud != null) hud.SetGameState(state.ToString());
        }
    }
}
