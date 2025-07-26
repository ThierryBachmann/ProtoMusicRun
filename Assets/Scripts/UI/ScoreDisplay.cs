using UnityEngine;
using TMPro;

namespace MusicRun
{
    public class ScoreDisplay : MonoBehaviour
    {
        public ScoreManager scoreManager;
        public PlayerController player;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI infoText;
        public GoalHandler goalHandler;
        private Color scoreGrowing;
        private Color scoreDecrease;
        private Color scoreFinal;
        private Color currentTextColor;
        private float lastScore;

        void Start()
        {
            ColorUtility.TryParseHtmlString("#00F20B", out scoreFinal);
            ColorUtility.TryParseHtmlString("#FF7D88", out scoreDecrease);
            ColorUtility.TryParseHtmlString("#FFEF2E", out scoreGrowing);
        }
        void Update()
        {
            //if (scoreManager)
            //    scoreText.text = $"{player.leaderboard.GetPlayerName()} Distance:{goalHandler.distance:N0} Score:{scoreManager.score:N0} Speed:{player.GetSpeed():N1} Multiplier:{player.speedMultiplier:N1}";
            //infoText.text = $"dir:{player.goalHandler.goalDirection:F2} angle:{player.goalHandler.goalAngle:F2}";
            Color targetColor = scoreText.color; // couleur par défaut

            //if (player.goalHandler.goalReached)
            //    targetColor = scoreFinal;
            //else
            {
                if (scoreManager.score > lastScore) targetColor = scoreGrowing;
                if (scoreManager.score < lastScore) targetColor = scoreDecrease;
            }

            if (currentTextColor != targetColor)
            {
                currentTextColor = targetColor;
                scoreText.color = currentTextColor;
            }
            lastScore = scoreManager.score;
        }
    }
}