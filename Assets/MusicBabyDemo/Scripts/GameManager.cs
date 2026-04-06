using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MusicRun
{
    /*
     * FILE ROLE
     * - Main orchestration part of GameManager (partial class).
     * - Holds shared state, scene references, and core game flow entry points.
     * - Other concerns are split into companion files (for example RuntimeHelpers).
     */
    public partial class GameManager : MonoBehaviour
    {
        public bool gameRunning;
        public bool awaitingPlayerStart;
        public bool levelRunning;
        public bool levelPaused;
        public bool levelFailed;
        public bool liteMode;
        public bool liteAutoSetting;
        public bool liteForceSetting;

        /// <summary>
        /// Index level from TerrainGenerator
        /// </summary>
        public int levelIndex;

        /// <summary>
        /// Player number of level 
        /// </summary>
        public int levelNumber;

        public float GoalPercentage;

        [Header("Debug")]
        public bool startAuto;
        public bool nextLevelAuto;
        public bool infoDebug;
        public bool enableShortcutKeys;
        public bool alwaysSucceed;
        public int FramePerSecond;
        private float deltaTimeFPS = 0.0f;
        public int LowerThresholdFPS = 28;
        public int HysteresisBandFPS = 4;

        [Header("GameObject reference")]
        public Camera cameraSkybox;
        public Camera cameraSolidColor;
        public Camera cameraSelected;

        public GoalHandler goalHandler;
        public GameObject startGameObject;
        public FirebaseLeaderboard leaderboard;
        public ScoreManager scoreManager;
        public BonusManager bonusManager;
        public HeaderDisplay headerDisplay;
        public LeaderboardDisplay leaderboardDisplay;
        public PlayerController playerController;
        public ActionGameDisplay actionGame;
        public ActionLevelDisplay actionLevel;
        public GoalReachedDisplay goalReachedDisplay;
        public TerrainGenerator terrainGenerator;
        public MidiManager midiManager;
        public SplashScreen splashScreen;
        public HelperScreen helperScreen;
        public SettingScreen settingScreen;
        public LevelFailedScreen levelFailedScreen;
        public SceneGoal sceneGoal;

        // Provide the game logic from the gamepad, see ActionGameDisplay
        public TouchEnabler touchEnabler;

        [Header("Level end prefab")]
        [Tooltip("Prefab instantiated in front of the player when the level fails.")]
        public GameObject VideoScreenPrefab;

        [Tooltip("Distance in meters in front of the player where the prefab will be spawned.")]
        public float VideoScreenDistance = 2.5f;

        // VideoScreenPrefab instance when the player failed to reaches the goal. 
        private GoalReachedDisplay goalReachedClone;

        void Awake()
        {
            // Subscribe to events
            settingScreen.OnSettingChange += OnSettingChange;
            leaderboard.OnLeaderboardLoaded += OnDisplayLeaderboard;
            leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
            goalHandler.OnGoalReached += OnLevelCompleted;
            midiManager.OnMusicEnded += OnLevelCompleted;
            leaderboard.OnLeaderboardLoaded += OnLeaderboardLoaded;
            leaderboard.OnScoreSubmitted += OnScoreSubmitted;
            //sceneGoal.OnClose += (ok) => { OnSwitchPause(false); };
            Utilities.Init();

        }

        void Start()
        {
            gameRunning = false;
            awaitingPlayerStart = false;
            levelRunning = false;
            levelPaused = false;
            levelFailed = false;

            //
            // Game logic from the gamepad when the start button is activated.
            //
            touchEnabler.controls.Gameplay.Start.performed += (InputAction.CallbackContext context) =>
            {
                if (!gameRunning)
                {
                    // Game is not running
                    GameStart();
                }
                else if (awaitingPlayerStart)
                {
                    // Will call StartLevel() when the scene goal is fully hidden.
                    sceneGoal.Hide();
                }
                else if (!levelRunning)
                {
                    // Game running, level is not running : player at the goal or failed.
                    if (levelFailed)
                        LevelRetry();
                    else
                        LevelNext();
                }
                else
                {
                    // Game and level are running.
                    if (levelPaused)
                        // but paused, so un-pause
                        OnSwitchPause(false);
                    else
                        // pause
                        OnSwitchPause(true);
                }

            };

            if (startAuto)
                GameStart();
            else
            {
                splashScreen.Show();
                actionLevel.Hide();
                actionGame.Show();
                actionGame.SelectActionsToShow();
                GamePresentation();
            }
            leaderboardDisplay.Hide();

            ////// Create first level, just to have a view, will be recreated when game starts.
            ////currentLevelIndex = terrainGenerator.SelectNextLevel(-1);
            ////terrainGenerator.CreateLevel(0);
        }

        public void GamePresentation()
        {
            Debug.Log("GameManger - arrival");
            // Now presentation until now, just generate terrain
            //GameStart();

        }
        public void GameStart()
        {
            Debug.Log("-level- StartGame levelNumber 1");
            //goalHandler.gameObject.SetActive(true);
            terrainGenerator.ResetTerrain();
            levelFailedScreen.Hide();
            // always increase along the game
            levelNumber = 1;
            // cycling around the game, but start at -1 to get level index 0
            levelIndex = terrainGenerator.CalculateNextLevel(-1);
            scoreManager.ScoreOverall = 0;
            LevelCreate(levelIndex);
        }

        public void GameStop()
        {
            Debug.Log($"-level- StopGame");

            // Cancel callback to StartLevel() when sceneGoal is closed
            sceneGoal.OnClose = null;
            // No, video issue if disabled
            //goalHandler.gameObject.SetActive(false);
            goalHandler.name = "Goal";
            startGameObject.transform.position = Vector3.zero;
            startGameObject.name = "Start";
            gameRunning = false;
            levelRunning = false;
            levelPaused = false;
            levelFailed = false;
            levelNumber = 1;
            actionLevel.Hide();
            actionGame.Show();
            SplashScreenDisplay();
            actionGame.SelectActionsToShow();
            actionGame.Show();
            playerController.ResetGameStop();
            terrainGenerator.ResetTerrain();
            GoalScreenHide();
        }


        /// <summary>
        /// Restart the current level.
        /// </summary>
        public void LevelRetry()
        {
            Debug.Log("-level- RetryLevel");
            LevelCreate(levelIndex, restartSame: true);
        }

        public void LevelNext()
        {
            Debug.Log("-level- NextLevel");
            levelNumber++;
            levelIndex = terrainGenerator.CalculateNextLevel(levelIndex);
            LevelCreate(levelIndex);
        }

        private void OnSettingChange()
        {
            Debug.Log("GameManager OnSettingChange");
            if (!liteAutoSetting)
                liteMode = liteForceSetting;
            else
                CalculateLiteMode();
            LiteModeApply();
        }

        public void LiteModeApply()
        {
            Debug.Log($"Lite mode: {liteMode}");

            //if (liteMode)
            //{
            //    headerDisplay.LiteModeDisplay(true);
            //    terrainGenerator.renderDistance = 1;
            //}
            //else
            //{
            //    headerDisplay.LiteModeDisplay(false);
            //    terrainGenerator.renderDistance = 5;
            //}
            SkyApply();
        }

        public void SkyApply()
        {
            if (liteMode)
            {
                cameraSkybox.enabled = false;
                cameraSolidColor.enabled = true;
                cameraSelected = cameraSolidColor;
            }
            else
            {
                cameraSolidColor.enabled = false;
                if (terrainGenerator.CurrentLevel != null && terrainGenerator.CurrentLevel.Skybox != null)
                {
                    foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                        camera.enabled = false;
                    terrainGenerator.CurrentLevel.Skybox.enabled = true;
                    cameraSelected = terrainGenerator.CurrentLevel.Skybox;
                }
                else
                {
                    cameraSkybox.enabled = true;
                    cameraSelected = cameraSkybox;
                }
            }
        }

        private void OnLeaderboardLoaded(List<LeaderboardPlayerScore> scores)
        {
            Debug.Log($"=== PLAYER RANK {playerController.playerName} === ");
            LeaderboardPlayerScore playerRank = scores.Find(s => s.playerName == playerController.playerName);
            if (playerRank != null)
                Debug.Log($"Player Rank {playerRank.playerName:20}: {playerRank.score} pts " +
                     $"(Time: {playerRank.completionTime:F1}s, Efficiency: {playerRank.pathEfficiency:F2})");
            else
                Debug.Log($"Player Rank not found {playerController.playerName:20}");

        }

        private void OnScoreSubmitted(bool success)
        {
            leaderboard.GetPlayerRank((s) =>
            {
                if (s != null)
                {
                    playerController.playerPosition = s.playerPosition;
                    playerController.playerBestScore = s.score;
                    leaderboardDisplay.RefreshPlayerScore(playerController);
                }
            });
        }

        private void OnDisplayLeaderboard(List<LeaderboardPlayerScore> scores)
        {
            //Debug.Log($"=== LEADERBOARD {scores.Count} entries ===");
            //for (int i = 0; i < scores.Count; i++)
            //{
            //    var score = scores[i];
            //    Debug.Log($"{i + 1}. {score.playerName:20}: {score.score} pts " +
            //             $"(Time: {score.completionTime:F1}s, Efficiency: {score.pathEfficiency:F2})");
            //}
        }

        private void OnScoreSubmissionResult(bool success)
        {
            if (success)
            {
                Debug.Log("Score submitted successfully!");
            }
            else
            {
                Debug.Log("Failed to submit score");
            }
        }

        public void OnSwitchPause(bool pause)
        {
            Debug.Log($"-level- OnSwitchPause pause:{pause} levelPaused was:{levelPaused}");
            if (pause)
            {
                midiManager.Pause();
                levelPaused = true;
            }
            else
            {
                midiManager.UnPause();
                levelPaused = false;
            }
            // Toggling the visibility and state of related UI elements. 
            actionLevel.ActivatePause(pause);
        }

        public void OnExitGame()
        {
            Debug.Log($"GameManager - OnExitGame");
            GameStop();
        }        void Update()
        {
            HandleDebugShortcuts();
            UpdateLevelProgressAndScore();
            CalculateFPS();
            CalculateLiteMode();
        }

        public void CalculateFPS()
        {
            // Exponential moving average for smoother results
            deltaTimeFPS += (Time.deltaTime - deltaTimeFPS) * 0.01f;
            FramePerSecond = (int)((1.0f / deltaTimeFPS));
        }
        public void CalculateLiteMode()
        {
            if (liteAutoSetting)
            {
                if (FramePerSecond < LowerThresholdFPS)
                {
                    if (!liteMode)
                    {
                        liteMode = true;
                        LiteModeApply();
                    }
                }
                if (FramePerSecond > LowerThresholdFPS + HysteresisBandFPS)
                {
                    if (liteMode)
                    {
                        liteMode = false;
                        LiteModeApply();
                    }
                }
            }
        }

        public IEnumerator ClearAndNextLevelTest()
        {
            terrainGenerator.ClearChunkPool();
            terrainGenerator.ClearChunks(0);
            levelNumber++;
            levelIndex = terrainGenerator.CalculateNextLevel(levelIndex);
            yield return null; // Wait one frame
            LevelCreate(levelIndex);
        }
    }
}
