using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MusicRun
{

    public class GameManager : MonoBehaviour
    {
        public bool gameRunning;
        public bool levelRunning;
        public bool levelFailed;
        public bool liteMode;
        public int currentLevelIndex;
        public int currentLevelNumber;
        public float MusicPercentage;
        public float GoalPercentage;

        [Header("Debug")]
        public bool startAuto;
        public bool nextLevelAuto;
        public bool infoDebug = false;

        [Header("GameObject reference")]
        public Camera cameraSkybox;
        public Camera cameraSoliColor;
        public GoalHandler goalHandler;
        public FirebaseLeaderboard leaderboard;
        public ScoreManager scoreManager;
        public HeaderDisplay headerDisplay;
        public LeaderboardDisplay leaderboardDisplay;
        public PlayerController playerController;
        public SoundManager soundManager;
        public ActionGameDisplay actionGame;
        public ActionLevelDisplay actionLevel;
        public GoalReachedDisplay goalReachedDisplay;
        public TerrainGenerator terrainGenerator;
        public MidiManager midiManager;
        public SplashScreen splashScreen;
        public HelperScreen helperScreen;
        public SettingScreen settingScreen;
        public LevelFailedScreen levelFailedScreen;

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
            levelFailed = false;
            if (startAuto)
                RestartGame();
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
            LiteModeApply();
        }

        public void LiteModeApply()
        {
            Debug.Log($"Lite mode: {liteMode}");

            if (liteMode)
            {
                headerDisplay.LiteModeDisplay(true);
                cameraSkybox.enabled = false;
                cameraSoliColor.enabled = true;
                terrainGenerator.renderDistance = 1;
            }
            else
            {
                headerDisplay.LiteModeDisplay(false);
                cameraSoliColor.enabled = false;
                cameraSkybox.enabled = true;
                terrainGenerator.renderDistance = 5;
            }
        }

        private void OnLeaderboardLoaded(List<LeaderboardPlayerScore> scores)
        {
            Debug.Log($"=== PLAYER RANK {leaderboard.GetPlayerName()} === ");
            LeaderboardPlayerScore playerRank = scores.Find(s => s.playerName == leaderboard.GetPlayerName());
            if (playerRank != null)
                Debug.Log($"Player Rank {playerRank.playerName:20}: {playerRank.score} pts " +
                     $"(Time: {playerRank.completionTime:F1}s, Efficiency: {playerRank.pathEfficiency:F2})");
            else
                Debug.Log($"Player Rank not found");

        }

        private void OnLevelCompleted(bool success)
        {
            scoreManager.CalculateScoreLevel(MusicPercentage, GoalPercentage);
            LeaderboardPlayerScore playerScore = new LeaderboardPlayerScore(
                         leaderboard.GetPlayerName(),
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
            headerDisplay.SetTitle();
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
            Debug.Log($"OnSwitchPause pause:{pause} levelRunning:{levelRunning}");
            if (pause)
            {
                midiManager.Pause();
                levelRunning = false;
            }
            else
            {
                midiManager.UnPause();
                levelRunning = true;
            }
        }
        public void OnExitGame()
        {
            StopGame();
        }

        void Update()
        {
            // Exemple : touche R pour redémarrer la partie
            //if (Input.GetKeyDown(KeyCode.H)) HelperScreenDisplay();

            if (Input.anyKeyDown)
            {
                foreach (char c in Input.inputString)
                {
                    if (c == 'h' || c == 'H') HelperScreenDisplay();
                    if (c == 'n' || c == 'N') StartCoroutine(ClearAndNextLevel());
                    if (c == 's' || c == 'S') StopGame();
                    if (c == 'r' || c == 'R') RestartGame();
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
            if (gameRunning && levelRunning)
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
        }

        public void HideAllPopups()
        {
            splashScreen.Hide();
            helperScreen.Hide();
            settingScreen.Hide();
            levelFailedScreen.Hide();
            levelFailedScreen.Hide();
            leaderboardDisplay.Hide();
        }

        public IEnumerator ClearAndNextLevel()
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

        public void RestartGame()
        {
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
            HideAllPopups();
            CreateAndStartLevel(currentLevelIndex, restartSame: true);
        }

        public void NextLevel()
        {
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
            Debug.Log($"CreateAndStartLevel {level}");
            scoreManager.ScoreLevel = 0;
            levelFailed = false;
            actionGame.Hide();
            actionLevel.Show();
            HideAllPopups();
            if (restartSame)
            {
                playerController.ResetPosition();
            }
            else
            {
                terrainGenerator.CreateLevel(level);
            }
            terrainGenerator.CreateLevel(level);
            goalHandler.NewLevel();
            goalReachedDisplay.NewLevel();
            midiManager.StartPlayMIDI(terrainGenerator.CurrentLevel.indexMIDI);
            gameRunning = true;
            levelRunning = true;
            playerController.LevelStarted();
        }

        public void StopGame()
        {
            gameRunning = false;
            levelRunning = false;
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