using UnityEngine;
using TMPro;

namespace MusicRun
{
    public class ScoreDisplay : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI infoText;
        private Color scoreGrowing;
        private Color scoreDecrease;
        private Color scoreFinal;
        private Color currentTextColor;
        private int lastScore;
        private GameManager gameManager;
        private ScoreManager scoreManager;
        private PlayerController player;
        private GoalHandler goalHandler;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager==null)
                return;
            scoreManager = gameManager.ScoreManager;
            player = gameManager.Player;
            goalHandler = gameManager.GoalHandler;
        }


        void Start()
        {
            ColorUtility.TryParseHtmlString("#00F20B", out scoreFinal);
            ColorUtility.TryParseHtmlString("#FF7D88", out scoreDecrease);
            ColorUtility.TryParseHtmlString("#FFEF2E", out scoreGrowing);
        }
        void Update()
        {
            scoreText.text = $"{gameManager.Leaderboard.GetPlayerName()} Distance:{goalHandler.distance:N0} Score:{scoreManager.ScoreLevel:N0} Speed:{player.GetSpeed():N1} Multiplier:{player.speedMultiplier:N1}";
            //infoText.text = $"dir:{player.goalHandler.goalDirection:F2} angle:{player.goalHandler.goalAngle:F2}";
            Color targetColor = scoreText.color; // couleur par défaut

            if (goalHandler.goalReached)
                targetColor = scoreFinal;
            else
            {
                if (scoreManager.ScoreLevel > lastScore) targetColor = scoreGrowing;
                if (scoreManager.ScoreLevel < lastScore) targetColor = scoreDecrease;
            }

            if (currentTextColor != targetColor)
            {
                currentTextColor = targetColor;
                scoreText.color = currentTextColor;
            }
            lastScore = scoreManager.ScoreLevel;
        }
    }
}