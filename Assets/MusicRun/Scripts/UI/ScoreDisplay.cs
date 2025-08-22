using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MusicRun
{
    public class ScoreDisplay : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI infoText;
        public Button quitButton;
        public Button directionButton;
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
            directionButton.onClick.AddListener(() =>
            {
                Debug.Log("directionButton");
                Application.OpenURL("https://https://paxstellar.fr/news/");
            });
            SetTitle();
        }

        public void SetTitle()
        {
            titleText.text = $"Music Run - {gameManager.leaderboard.GetPlayerName()}";
        }

        void Update()
        {
            float score = scoreManager.CalculateScoreGoal(gameManager.MusicPercentage, gameManager.GoalPercentage);
            // Level: 1 Score:0 Bonus: 0 Game Score:0
            scoreText.text = $"Level: {gameManager.currentLevelNumber} Score:{score:N0} Bonus: {scoreManager.ScoreBonus+(int)scoreManager.bonusInProgress} Game Score:{scoreManager.ScoreOverall:N0}";
            //scoreText.text = $"{gameManager.leaderboard.GetPlayerName()} Level: {gameManager.currentLevelNumber} Score:{scoreManager.ScoreOverall:N0} Bonus: {scoreManager.ScoreBonus} Speed:{player.GetSpeed():N1}";
            infoText.text = $"Debug index level:{gameManager.currentLevelIndex} dir:{goalHandler.goalDirection:F2} angle:{goalHandler.goalAngle:F2}";
            Color targetColor = scoreText.color; // couleur par défaut
            directionButton.transform.localRotation = Quaternion.Euler(0f, 0f, -goalHandler.goalAngle);
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