using UnityEngine;
using MidiPlayerTK;
using System.Collections.Generic; // si tu veux redémarrer la musique

public class GameManager : MonoBehaviour
{
    public bool gameRunning;
    public bool levelRunning;
    public bool startAuto;

    [Header("GameObject")]
    public GoalHandler goalHandler;
    public FirebaseLeaderboard leaderboard;
    public ScoreManager scoreManager;
    public LeaderboardDisplay leaderboardDisplay;
    public PlayerController player;
    public MidiFilePlayer midiPlayer;
    public Transform startPosition;
    public ActionDisplay actionDisplay;
    public GoalReachedDisplay goalReachedDisplay;

    void Awake()
    {
        // Subscribe to events
        leaderboard.OnLeaderboardLoaded += OnDisplayLeaderboard;
        leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
        goalHandler.OnLevelCompleted += OnLevelCompleted;
        leaderboard.OnLeaderboardLoaded += OnLeaderboardLoaded;
        leaderboard.OnScoreSubmitted += OnScoreSubmitted;
    }
    void Start()
    {
        gameRunning = false;
        levelRunning = false;
        if (startAuto)
            RestartGame();
        else
            actionDisplay.Show();
        leaderboardDisplay.Hide();
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
        LeaderboardPlayerScore playerScore = new LeaderboardPlayerScore(
                     leaderboard.GetPlayerName(),
                     scoreManager.score,
                     999,
                     999,
                     999,
                     1
                      );
        leaderboard.SubmitScore(playerScore);
        levelRunning = false;
        actionDisplay.Show();
        //leaderboardDisplay.Show(this);
    }

    private void OnScoreSubmitted(bool success)
    {
        leaderboard.GetPlayerRank((s) =>
        {
            if (s != null)
            {
                player.playerPosition = s.playerPosition;
                player.playerBestScore = s.score;
                leaderboardDisplay.RefreshPlayerScore(player);
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
    }

    public void LeaderboardSwitchDisplay()
    {
        leaderboardDisplay.SwitchVisible(player);
    }

    public void RestartGame()
    {

        actionDisplay.Hide();
        leaderboardDisplay.Hide();
        player.ResetPlayer(startPosition);
        goalHandler.goalReached = false;
        goalReachedDisplay.Reset();
        scoreManager.score = 0;

        if (midiPlayer != null)
        {
            midiPlayer.MPTK_Stop();
            midiPlayer.MPTK_RePlay();
        }
        gameRunning = true;
        levelRunning = true;
    }
    public void NextLevel()
    {
        actionDisplay.Hide();
        leaderboardDisplay.Hide();
        player.ResetPosition(startPosition);
        goalHandler.goalReached = false;
        goalReachedDisplay.Reset();
        scoreManager.score = 0;

        if (midiPlayer != null)
        {
            midiPlayer.MPTK_Stop();
            midiPlayer.MPTK_RePlay(); 
        }
        gameRunning = true;
        levelRunning = true;
    }

    public void StopGame()
    {
        actionDisplay.Show();
        leaderboardDisplay.Hide();
        gameRunning = false;
        levelRunning = false;
        actionDisplay.Show();
    }
}
