using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public ScoreManager scoreManager;
    public PlayerController player;
    public TextMeshProUGUI scoreText;
    public GoalHandler goalHandler;
    private Color scoreGrowing;
    private Color scoreDecrease;
    private float lastScore;
    private Color currentTextColor;

    void Start()
    {
        ColorUtility.TryParseHtmlString("#00F20B", out scoreGrowing);
        ColorUtility.TryParseHtmlString("#FF7D88", out scoreDecrease);
    }
    void Update()
    {
        if (scoreManager)
            scoreText.text = $"Distance:{goalHandler.distance:N0} Score:{scoreManager.score:N0} Speed:{player.GetSpeed() * 10f:N0} Multiplier:{player.speedMultiplier * 10f:N0} {player.goalHandler.goalDirection:F2} {player.goalHandler.goalAngle:F2}";

        Color targetColor = scoreText.color; // couleur par défaut

        if (scoreManager.score > lastScore) targetColor = scoreGrowing;
        if (scoreManager.score < lastScore) targetColor = scoreDecrease;

        if (currentTextColor != targetColor)
        {
            currentTextColor = targetColor;
            scoreText.color = currentTextColor;
        }
        lastScore = scoreManager.score;
    }
}