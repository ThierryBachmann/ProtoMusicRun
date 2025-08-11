using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicRun
{

    public class GameManager : MonoBehaviour
    {
        public bool gameRunning;
        public bool levelRunning;
        public int currentLevelIndex;
        public int currentLevelNumber;
        public bool startAuto;
        public bool nextLevelAuto;
        public float MusicPercentage;
        public float GoalPercentage;

        [Header("GameObject reference")]
        public GoalHandler goalHandler;
        public FirebaseLeaderboard leaderboard;
        public ScoreManager scoreManager;
        public LeaderboardDisplay leaderboardDisplay;
        public PlayerController playerController;
        public MidiFilePlayer midiPlayer;
        public SoundManager soundManager;
        public ActionDisplay actionDisplay;
        public PanelDisplay actionPlay;
        public GoalReachedDisplay goalReachedDisplay;
        public TerrainGenerator terrainGenerator;
        public MidiTempoSync midiTempoSync;
        public SwitchButton pauseButton;

        public int[] channelPlayed = new int[16]; // Array to track which channels are currently playing
        void Awake()
        {
            // Subscribe to events
            leaderboard.OnLeaderboardLoaded += OnDisplayLeaderboard;
            leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
            goalHandler.OnLevelCompleted += OnLevelCompleted;
            leaderboard.OnLeaderboardLoaded += OnLeaderboardLoaded;
            leaderboard.OnScoreSubmitted += OnScoreSubmitted;
            Utilities.Init();
            midiPlayer.OnEventStartPlayMidi.AddListener((name) =>
            {
                // Start of the MIDI playback has been triggered.
                Debug.Log($"MidiPlayer Play MIDI '{name}' {goalHandler.distanceAtStart}");
                // Reset some MIDI properties which can be done only when MIDI playback is started.
                midiTempoSync.Reset();
                StartCoroutine(UpdateMaxDistanceMPTK());
                midiPlayer.MPTK_Transpose = 0;
                midiPlayer.MPTK_MidiAutoRestart = false;
                Array.Clear(channelPlayed, 0, 16);
            });
            midiPlayer.OnEventNotesMidi.AddListener((midiEvents) =>
            {
                // Handle MIDI events if needed
                // Debug.Log($"MidiPlayer Notes: {midiEvents.Count}");
                foreach (var midiEvent in midiEvents)
                    channelPlayed[midiEvent.Channel]++;
            });
        }

        private IEnumerator UpdateMaxDistanceMPTK()
        {
            // Wait for the goalHandler to have a valid distanceAtStart.
            while (goalHandler.distanceAtStart < 0)
                yield return new WaitForSeconds(0.1f);
            // Attenuation of volume with the distance from the player and the goal.
            // When the player is at the start, the volume is 5% of the volume max at the goal.
            midiPlayer.MPTK_MaxDistance = goalHandler.distanceAtStart * 1.05f;
            Debug.Log($"MaxDistance set {midiPlayer.MPTK_MaxDistance}");
        }

        void Start()
        {
            gameRunning = false;
            levelRunning = false;
            if (startAuto)
                RestartGame();
            else
            {
                actionPlay.Hide();
                actionDisplay.Show();
                actionDisplay.SelectActionsToShow();
            }
            leaderboardDisplay.Hide();

            ////// Create first level, just to have a view, will be recreated when game starts.
            ////currentLevelIndex = terrainGenerator.SelectNextLevel(-1);
            ////terrainGenerator.CreateLevel(0);
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
                StartCoroutine(Utilities.WaitAndCall(2.5f, NextLevel));
            }
            else
            {
                actionPlay.Hide();
                actionDisplay.Show();
                actionDisplay.SelectActionsToShow();
            }
            //leaderboardDisplay.Show(this);
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
                // Show success UI
            }
            else
            {
                Debug.Log("Failed to submit score");
                // Show error UI
            }
        }
        void Update()
        {
            // Exemple : touche R pour redémarrer la partie
            if (Input.GetKeyDown(KeyCode.C)) NextLevel();
            if (Input.GetKeyDown(KeyCode.S)) StopGame();
            if (Input.GetKeyDown(KeyCode.R)) RestartGame();
            if (Input.GetKeyDown(KeyCode.A)) actionDisplay.SwitchVisible();
            if (Input.GetKeyDown(KeyCode.L)) LeaderboardSwitchDisplay();

            if (pauseButton.IsOn && gameRunning)
            {
                if (levelRunning)
                {
                    midiPlayer.MPTK_Pause();
                    levelRunning = false;
                    //    actionDisplay.Show();
                }
            }

            if (!pauseButton.IsOn && gameRunning)
            {
                if (!levelRunning)
                {
                    midiPlayer.MPTK_UnPause();
                    levelRunning = true;
                    //    actionDisplay.Hide();
                }
            }

            if (gameRunning && levelRunning)
            {
                if (goalHandler.distanceAtStart > 0)
                    GoalPercentage = 100f - (goalHandler.distance / goalHandler.distanceAtStart * 100f);
                else
                    GoalPercentage = 0f;
                MusicPercentage = ((float)midiPlayer.MPTK_TickCurrent / (float)midiPlayer.MPTK_TickLastNote) * 100f;
                scoreManager.ScoreGoal = scoreManager.CalculateScoreGoal(MusicPercentage, GoalPercentage);
            }
        }

        public void LeaderboardSwitchDisplay()
        {
            leaderboardDisplay.SwitchVisible(playerController);
        }

        public void RestartGame()
        {
            currentLevelNumber = 1;
            currentLevelIndex = terrainGenerator.SelectNextLevel(-1);
            scoreManager.ScoreOverall = 0;
            CreateAndStartLevel(currentLevelIndex);
        }

        /// <summary>
        /// Restart the current level.
        /// </summary>
        public void RestartLevel()
        {
            CreateAndStartLevel(currentLevelIndex, restartSame : true);
        }

        public void NextLevel()
        {
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
            scoreManager.ScoreLevel = 0;
            actionDisplay.Hide();
            actionPlay.Show();
            leaderboardDisplay.Hide();
            if (restartSame)
            {
                playerController.ResetPosition();
            }
            else
            {
                terrainGenerator.CreateLevel(level);
            }
            terrainGenerator.CreateLevel(level);
            playerController.LevelStarted();
            goalHandler.NewLevel();
            goalReachedDisplay.NewLevel();
            midiPlayer.MPTK_MidiIndex = terrainGenerator.CurrentLevel.indexMIDI;
            if (midiPlayer != null)
            {
                midiPlayer.MPTK_Stop();
                midiPlayer.MPTK_Play();
            }
            gameRunning = true;
            levelRunning = true;
        }

        public void StopGame()
        {
            actionPlay.Hide();
            actionDisplay.Show();
            leaderboardDisplay.Hide();
            gameRunning = false;
            levelRunning = false;
            actionDisplay.Show();
        }
    }
}