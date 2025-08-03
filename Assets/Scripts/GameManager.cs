using UnityEngine;
using MidiPlayerTK;
using System.Collections.Generic;

namespace MusicRun
{

    public class GameManager : MonoBehaviour
    {
        public bool gameRunning;
        public bool levelRunning;
        public int currentLeveIndex;
        public bool startAuto;
        public bool nextLevelAuto;
        public float MusicPercentage;
        public float GoalPercentage;

        [Header("GameObject reference")]
        public GoalHandler GoalHandler;
        public FirebaseLeaderboard Leaderboard;
        public ScoreManager ScoreManager;
        public LeaderboardDisplay LeaderboardDisplay;
        public PlayerController Player;
        public MidiFilePlayer MidiPlayer;
        public SoundManager SoundManager;
        public ActionDisplay ActionDisplay;
        public GoalReachedDisplay GoalReachedDisplay;
        public TerrainGenerator TerrainGenerator;
        public MidiTempoSync MidiTempoSync;

        void Awake()
        {
            // Subscribe to events
            Leaderboard.OnLeaderboardLoaded += OnDisplayLeaderboard;
            Leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
            GoalHandler.OnLevelCompleted += OnLevelCompleted;
            Leaderboard.OnLeaderboardLoaded += OnLeaderboardLoaded;
            Leaderboard.OnScoreSubmitted += OnScoreSubmitted;
            Utilities.Init();
        }
        void Start()
        {
            gameRunning = false;
            levelRunning = false;
            if (startAuto)
                RestartGame();
            else
                ActionDisplay.Show();
            LeaderboardDisplay.Hide();
            TerrainGenerator.CreateLevel(0);
        }

        private void OnLeaderboardLoaded(List<LeaderboardPlayerScore> scores)
        {
            Debug.Log($"=== PLAYER RANK {Leaderboard.GetPlayerName()} === ");
            LeaderboardPlayerScore playerRank = scores.Find(s => s.playerName == Leaderboard.GetPlayerName());
            if (playerRank != null)
                Debug.Log($"Player Rank {playerRank.playerName:20}: {playerRank.score} pts " +
                     $"(Time: {playerRank.completionTime:F1}s, Efficiency: {playerRank.pathEfficiency:F2})");
            else
                Debug.Log($"Player Rank not found");

        }

        private void OnLevelCompleted(bool success)
        {
            ScoreManager.CalculateLevelScore(MusicPercentage, GoalPercentage);
            LeaderboardPlayerScore playerScore = new LeaderboardPlayerScore(
                         Leaderboard.GetPlayerName(),
                         ScoreManager.ScoreLevel,
                         999,
                         999,
                         999,
                         1
                          );
            Leaderboard.SubmitScore(playerScore);
            levelRunning = false;
            if (nextLevelAuto)
                StartCoroutine(Utilities.WaitAndCall(2.5f, NextLevel));
            else
                ActionDisplay.Show();
            //leaderboardDisplay.Show(this);
        }

        private void OnScoreSubmitted(bool success)
        {
            Leaderboard.GetPlayerRank((s) =>
            {
                if (s != null)
                {
                    Player.playerPosition = s.playerPosition;
                    Player.playerBestScore = s.score;
                    LeaderboardDisplay.RefreshPlayerScore(Player);
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
            if (Input.GetKeyDown(KeyCode.A)) ActionDisplay.SwitchVisible();
            if (Input.GetKeyDown(KeyCode.L)) LeaderboardSwitchDisplay();

            if (gameRunning && levelRunning)
            {
                if (GoalHandler.distanceAtStart > 0)
                    GoalPercentage = 100f - (GoalHandler.distance / GoalHandler.distanceAtStart * 100f);
                else
                    GoalPercentage = 0f;
                MusicPercentage = ((float)MidiPlayer.MPTK_TickCurrent / (float)MidiPlayer.MPTK_TickLastNote) * 100f;
            }
        }

        public void LeaderboardSwitchDisplay()
        {
            LeaderboardDisplay.SwitchVisible(Player);
        }

        public void RestartGame()
        {
            currentLeveIndex = 0;
            ScoreManager.ScoreOverall = 0;
            StartLevel();
        }
        public void NextLevel()
        {
            currentLeveIndex++;
            StartLevel();
        }

        private void StartLevel()
        {
            ScoreManager.ScoreLevel = 0;
            ActionDisplay.Hide();
            LeaderboardDisplay.Hide();
            TerrainGenerator.CreateLevel(currentLeveIndex);
            Player.LevelStarted();
            GoalHandler.NewLevel();
            GoalReachedDisplay.NewLevel();
            GoalHandler.NewLevel();
            MidiTempoSync.Reset();
            if (MidiPlayer != null)
            {
                MidiPlayer.MPTK_Stop();
                MidiPlayer.MPTK_RePlay();
            }
            gameRunning = true;
            levelRunning = true;
        }

        public void StopGame()
        {
            ActionDisplay.Show();
            LeaderboardDisplay.Hide();
            gameRunning = false;
            levelRunning = false;
            ActionDisplay.Show();
        }
    }
}