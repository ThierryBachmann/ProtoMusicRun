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

    void Start()
    {
        // Subscribe to events
        leaderboard.OnLeaderboardLoaded += DisplayLeaderboard;
        leaderboard.OnScoreSubmitted += OnScoreSubmissionResult;
    }

    public void OnGameComplete(long score, float time, float efficiency, float maxSpeed, int level)
    {
        PlayerScore playerScore = new PlayerScore(
           leaderboard.firebaseAuth.GetPlayerName(),
           score,
           time,
           efficiency,
           maxSpeed,
           level
            );
        leaderboard.SubmitScore(playerScore);
    }

    private void DisplayLeaderboard(List<PlayerScore> scores)
    {
        Debug.Log($"=== LEADERBOARD {scores.Count} entries === ");
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
        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();
    }

    public void RestartGame()
    {
        // Reset du joueur
        player.ResetPosition(startPosition);
        player.speedMultiplier = 1f;
        player.goalHandler.goalReached = false;
        // Réinitialisation du score
        scoreManager.score = 0;

        // Réinitialisation de la musique
        if (midiPlayer != null)
        {
            midiPlayer.MPTK_Stop();
            midiPlayer.MPTK_RePlay(); // ou MPTK_Play() si tu préfères
        }

        // Autres reset possibles ici
    }
}
