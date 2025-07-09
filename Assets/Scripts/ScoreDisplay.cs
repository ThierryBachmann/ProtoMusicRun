using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public ScoreManager scoreManager;
    public PlayerController player;
    public TextMeshProUGUI scoreText;
    public GoalHandler goalHandler;
    Color scoreGrowing;
    Color scoreDecrease;
    float lastScore;

    void Start()
    {
        ColorUtility.TryParseHtmlString("#00F20B", out scoreGrowing);
        ColorUtility.TryParseHtmlString("#FF7D88", out scoreDecrease);
    }
    void Update()
    {
        if (scoreManager)
            scoreText.text = $"Distance:{goalHandler.distance:N0} Score:{scoreManager.score:N0} Speed:{player.GetSpeed() * 10f:N0} Multiplier:{player.speedMultiplier * 10f:N0} {player.goalHandler.goalDirection:F2} {player.goalHandler.goalAngle:F2}";
        if (scoreManager.score > lastScore) scoreText.color = scoreGrowing;
        if (scoreManager.score < lastScore) scoreText.color = scoreDecrease;
        lastScore = scoreManager.score;
    }
}