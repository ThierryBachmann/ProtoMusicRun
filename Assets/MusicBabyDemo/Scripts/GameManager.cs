using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MusicRun
{

    public class GameManager : MonoBehaviour
    {
        public bool gameRunning;
        public bool levelRunning;
        public bool levelPaused;
        public bool levelFailed;
        public bool liteMode;
        public bool liteAutoSetting;
        public bool liteForceSetting;
        public int currentLevelIndex;
        public int currentLevelNumber;
        public float MusicPercentage;
        public float GoalPercentage;

        [Header("Debug")]
        public bool startAuto;
        public bool nextLevelAuto;
        public bool infoDebug = false;
        public bool enableShortcutKeys = false;
        public int FramePerSecond;
        private float deltaTimeFPS = 0.0f;
        public int LowerThresholdFPS = 28;
        public int HysteresisBandFPS = 4;

        [Header("GameObject reference")]
        public Camera cameraSkybox;
        public Camera cameraSolidColor;
        public Camera cameraSelected;

        public GoalHandler goalHandler;
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
        public TouchEnabler touchEnabler;
        public SceneGoal sceneGoal;

        void Awake()
        {
            // Subscribe to events
            settingScreen.OnSettingChange += OnSettingChange;
            leaderboard.OnLeaderboardLoaded += OnDisplayLeaderboard;
            leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
            goalHandler.OnLevelCompleted += OnLevelCompleted;
            leaderboard.OnLeaderboardLoaded += OnLeaderboardLoaded;
            leaderboard.OnScoreSubmitted += OnScoreSubmitted;
            Utilities.Init();

        }

        void Start()
        {
            gameRunning = false;
            levelRunning = false;
            levelPaused = false;
            levelFailed = false;
            if (startAuto)
                StartGame();
            else
            {
                splashScreen.Show();
                actionLevel.Hide();
                actionGame.Show();
                actionGame.SelectActionsToShow();
            }
            leaderboardDisplay.Hide();

            ////// Create first level, just to have a view, will be recreated when game starts.
            ////currentLevelIndex = terrainGenerator.SelectNextLevel(-1);
            ////terrainGenerator.CreateLevel(0);
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

            if (liteMode)
            {
                headerDisplay.LiteModeDisplay(true);
                terrainGenerator.renderDistance = 1;
            }
            else
            {
                headerDisplay.LiteModeDisplay(false);
                terrainGenerator.renderDistance = 5;
            }
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

        private void OnLevelCompleted(bool success)
        {
            scoreManager.CalculateScoreLevel(MusicPercentage, GoalPercentage);
            LeaderboardPlayerScore playerScore = new LeaderboardPlayerScore(
                        leaderboard.firebaseAuth.GetUserId(),
                        playerController.playerName,
                        scoreManager.ScoreLevel,
                        999,
                        999,
                        999,
                        1
                        );
            leaderboard.SubmitScore(playerScore);
            levelRunning = false;

            // Clear chunk as sooner as posible to avoid collision meshes stay when building next level
            terrainGenerator.ClearChunks(1);
            if (nextLevelAuto)
            {
                StartCoroutine(Utilities.WaitAndCall(2500f, NextLevel));
            }
            else
            {
                actionLevel.Hide();
                actionGame.Show();
                actionGame.SelectActionsToShow();
            }
            playerController.LevelCompleted();
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
            Debug.Log($"=== LEADERBOARD {scores.Count} entries ===");
            for (int i = 0; i < scores.Count; i++)
            {
                var score = scores[i];
                Debug.Log($"{i + 1}. {score.playerName:20}: {score.score} pts " +
                         $"(Time: {score.completionTime:F1}s, Efficiency: {score.pathEfficiency:F2})");
            }
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
            StopGame();
        }

        void Update()
        {
            // Exemple : touche R pour redémarrer la partie
            //if (Input.GetKeyDown(KeyCode.H)) HelperScreenDisplay();

            if (enableShortcutKeys)
                if (Input.anyKeyDown)
                {
                    foreach (char c in Input.inputString)
                    {
                        if (c == 'h' || c == 'H') HelperScreenDisplay();
                        if (c == 'n' || c == 'N') StartCoroutine(ClearAndNextLevelTest());
                        if (c == 's' || c == 'S') StopGame();
                        if (c == 'r' || c == 'R') StartGame();
                        if (c == 'a' || c == 'A') actionGame.SwitchVisible();
                        if (c == 'l' || c == 'L') LeaderboardSwitchDisplay();
                        if (c == 'm' || c == 'M') midiManager.SoundOnOff();
                        if (c == 'g' || c == 'G')
                        {
                            terrainGenerator.ClearChunks(0);
                            terrainGenerator.UpdateChunks(playerController.CurrentPlayerChunk);
                        }
                    }
                }

            // Calculate level progression and intermediary score
            if (gameRunning && levelRunning && !levelPaused)
            {
                if (goalHandler.distanceAtStart > 0)
                    GoalPercentage = 100f - (goalHandler.distance / goalHandler.distanceAtStart * 100f);
                else
                    GoalPercentage = 0f;
                MusicPercentage = midiManager.Progress;
                scoreManager.ScoreGoal = scoreManager.CalculateScoreGoal(MusicPercentage, GoalPercentage);

                // Check level failed
                if (MusicPercentage >= 100f && GoalPercentage <= 98f)
                {
                    levelFailed = true;
                    levelRunning = false;
                    HideAllPopups();
                    levelFailedScreen.Show();
                    actionLevel.Hide();
                    actionGame.Show();
                    actionGame.SelectActionsToShow();
                }
            }

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

        public void HideAllPopups()
        {
            splashScreen.Hide();
            helperScreen.Hide();
            settingScreen.Hide();
            levelFailedScreen.Hide();
            levelFailedScreen.Hide();
            leaderboardDisplay.Hide();
            sceneGoal.Hide();
        }

        public IEnumerator ClearAndNextLevelTest()
        {
            terrainGenerator.ClearChunks(0);
            currentLevelNumber++;
            currentLevelIndex = terrainGenerator.SelectNextLevel(currentLevelIndex);
            yield return null; // Wait one frame
            CreateAndStartLevel(currentLevelIndex);
        }
        public void LeaderboardSwitchDisplay()
        {
            HideAllPopups();
            leaderboardDisplay.SwitchVisible(playerController);
        }

        public void StartGame()
        {
            Debug.Log("-level- StartGame Level 1");
            goalHandler.gameObject.SetActive(true);
            terrainGenerator.ResetTerrain();
            HideAllPopups();
            levelFailedScreen.Hide();
            currentLevelNumber = 1; // always increase along the game
            currentLevelIndex = terrainGenerator.SelectNextLevel(-1); // cycling around the game
            scoreManager.ScoreOverall = 0;
            CreateAndStartLevel(currentLevelIndex);
        }

        /// <summary>
        /// Restart the current level.
        /// </summary>
        public void RetryLevel()
        {
            Debug.Log("-level- RetryLevel");
            HideAllPopups();
            CreateAndStartLevel(currentLevelIndex, restartSame: true);
        }

        public void NextLevel()
        {
            Debug.Log("-level- NextLevel");
            HideAllPopups();
            currentLevelNumber++;
            currentLevelIndex = terrainGenerator.SelectNextLevel(currentLevelIndex);
            CreateAndStartLevel(currentLevelIndex);
        }

        /// <summary>
        /// Create and start a new level or restart the same level (preserve generated chunks).
        /// </summary>
        /// <param name="level"></param>
        /// <param name="restartSame"></param>
        private void CreateAndStartLevel(int level, bool restartSame = false)
        {
            Debug.Log($"-level- CreateAndStartLevel {level}");
            scoreManager.ScoreLevel = 0;
            levelFailed = false;
            actionGame.Hide();
            actionLevel.Show();
            HideAllPopups();

            // Need to get the level description before to get information about instrument in CreateLevel.
            midiManager.LoadMIDI(terrainGenerator.levels[level]);
            midiManager.BuildMidiChannel(terrainGenerator.levels[level]);

            if (restartSame)
            {
                playerController.ResetPosition();
            }
            else
            {
                terrainGenerator.CreateLevel(level);
            }

            // The scene must be loaded before playing the MIDI.
            midiManager.PlayMIDI();

            goalHandler.gameObject.SetActive(true);
            goalHandler.NewLevel();
            goalReachedDisplay.NewLevel();
            gameRunning = true;
            levelRunning = true;
            OnSwitchPause(true);
            playerController.LevelStarted();
            sceneGoal.OnClose += (ok) => { OnSwitchPause(false); };
            sceneGoal.SetInfo(terrainGenerator.CurrentLevel.name, terrainGenerator.CurrentLevel.description);
            sceneGoal.Show();
        }

        public void StopGame()
        {
            goalHandler.gameObject.SetActive(false);
            gameRunning = false;
            levelRunning = false;
            levelPaused = false;
            levelFailed = false;
            actionLevel.Hide();
            actionGame.Show();
            SplashScreenDisplay();
            actionGame.Show();
            playerController.ResetPosition();
            terrainGenerator.ResetTerrain();
        }

        public void HelperScreenDisplay()
        {
            HideAllPopups();
            helperScreen.SwitchVisible();
        }
        public void SplashScreenDisplay()
        {
            HideAllPopups();
            splashScreen.SwitchVisible();
        }
    }
}