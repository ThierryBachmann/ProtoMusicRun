using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public ScoreManager scoreManager;
    public PlayerController player;
    public TextMeshProUGUI scoreText;
    public GoalHandler goalHandler;

    void Update()
    {
        if (scoreManager)
            scoreText.text = $"Distance: {goalHandler.distance:N0}   Score: {scoreManager.score:N0}    Speed: {player.GetSpeed()*10f:N0}    Multiplier:{player.speedMultiplier*10f:N0} {player.goalHandler.forward:F2}"; // format 1 000
    }
}