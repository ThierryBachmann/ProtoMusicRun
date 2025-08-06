using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MusicRun
{
    public class ScoreDisplay : MonoBehaviour
    {
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI infoText;
        public Button quitButton;
        public Button helpButton;
        private Color currentTextColor;
        private GameManager gameManager;
        private ScoreManager scoreManager;
        private PlayerController player;
        private GoalHandler goalHandler;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            scoreManager = gameManager.scoreManager;
            player = gameManager.playerController;
            goalHandler = gameManager.goalHandler;
        }

        void Start()
        {
            quitButton.onClick.AddListener(() =>
            {
                Debug.Log("quitButton");
                Application.Quit();
            });
            helpButton.onClick.AddListener(() =>
            {
                Debug.Log("helpButton");
                Application.OpenURL("https://https://paxstellar.fr/news/");
            });
        }

        void Update()
        {
            scoreText.text = $"{gameManager.leaderboard.GetPlayerName()} Level: {gameManager.currentLeveIndex+1} Score:{scoreManager.ScoreOverall:N0} Bonus: {scoreManager.ScoreBonus} Speed:{player.GetSpeed():N1}";
            //infoText.text = $"dir:{player.goalHandler.goalDirection:F2} angle:{player.goalHandler.goalAngle:F2}";
            Color targetColor = scoreText.color; // couleur par défaut

            if (goalHandler.goalReached)
                targetColor = Utilities.ColorGreen;
            else
            {
                targetColor = scoreManager.CalculateColor();
            }

            if (currentTextColor != targetColor)
            {
                currentTextColor = targetColor;
                scoreText.color = currentTextColor;
            }
        }


    }
}