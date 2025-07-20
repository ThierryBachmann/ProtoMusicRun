using UnityEngine;
using MidiPlayerTK;
using System.Collections.Generic; // si tu veux redémarrer la musique

public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public ScoreManager scoreManager;
    public MidiFilePlayer midiPlayer; // optionnel
    public Transform startPosition;
    public FirebaseLeaderboard leaderboard;
    public LeaderboardDisplay leaderboardDisplay;
    public ActionDisplay actionDisplay;
    public bool gameRunning;
    public bool levelRunning;
    public bool startAuto;

    void Awake()
    {
        // Subscribe to events
        leaderboard.OnLeaderboardLoaded += OnDisplayLeaderboard;
        leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
    }
    void Start()
    {
        gameRunning = false;
        levelRunning = false;
        if (startAuto)
            RestartGame();
        else
            actionDisplay.Show();
    }

    private void OnDisplayLeaderboard(List<PlayerScore> scores)
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
        if (Input.GetKeyDown(KeyCode.S)) Stop();
        if (Input.GetKeyDown(KeyCode.R)) RestartGame();
        if (Input.GetKeyDown(KeyCode.A)) actionDisplay.SwitchVisible();
        if (Input.GetKeyDown(KeyCode.L)) LeaderboardSwitchDisplay();
    }

    public void LeaderboardSwitchDisplay()
    {
        leaderboardDisplay.SwitchVisible(player);
    }

    public void LevelCompleted()
    {
        levelRunning = false;
        actionDisplay.Show();
    }


    public void RestartGame()
    {

        actionDisplay.Hide();
        leaderboardDisplay.Hide();
        // Reset du joueur
        player.ResetPosition(startPosition);
        player.speedMultiplier = 0.5f;
        player.goalHandler.goalReached = false;
        // Réinitialisation du score
        scoreManager.score = 0;

        // Réinitialisation de la musique
        if (midiPlayer != null)
        {
            midiPlayer.MPTK_Stop();
            midiPlayer.MPTK_RePlay(); // ou MPTK_Play() si tu préfères
        }
        gameRunning = true;
        levelRunning = true;
        // Autres reset possibles ici
    }
    public void NextLevel()
    {
        actionDisplay.Hide();
        leaderboardDisplay.Hide();
        // Reset du joueur
        player.ResetPosition(startPosition);
        player.speedMultiplier = 0.5f;
        player.goalHandler.goalReached = false;
        // Réinitialisation du score
        scoreManager.score = 0;

        // Réinitialisation de la musique
        if (midiPlayer != null)
        {
            midiPlayer.MPTK_Stop();
            midiPlayer.MPTK_RePlay(); // ou MPTK_Play() si tu préfères
        }
        gameRunning = true;
        levelRunning = true;
        // Autres reset possibles ici
    }
    public void Stop()
    {
        actionDisplay.Show();
        leaderboardDisplay.Hide();
        gameRunning = false;
        levelRunning = false;
        actionDisplay.Show();
    }
}
