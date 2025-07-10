using UnityEngine;
using MidiPlayerTK; // si tu veux redémarrer la musique

public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public ScoreManager scoreManager;
    public MidiFilePlayer midiPlayer; // optionnel
    public Transform startPosition;

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
