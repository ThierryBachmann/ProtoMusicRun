using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public ScoreManager scoreManager;
    public PlayerController player;
    public TextMeshProUGUI scoreText;

    void Update()
    {
        if (scoreManager)
            scoreText.text = $"Score: {scoreManager.score:N0}    Speed: {player.GetSpeed():N1}    Multiplier:{player.speedMultiplier:N1}"; // format 1 000
    }
}