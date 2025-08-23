using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MusicRun
{
    public class HeaderDisplay : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TitleItem itemLevel;
        public TitleItem itemScore;
        public TitleItem itemBonus;

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

            //scoreText.text = $"{gameManager.leaderboard.GetPlayerName()} Level: {gameManager.currentLevelNumber} Score:{scoreManager.ScoreOverall:N0} Bonus: {scoreManager.ScoreBonus} Speed:{player.GetSpeed():N1}";
            //infoText.text = $"Debug index level:{gameManager.currentLevelIndex} dir:{goalHandler.goalDirection:F2} angle:{goalHandler.goalAngle:F2}";

            infoText.text = $"Debug index level:{gameManager.currentLevelIndex} chunkCreatedCount:{gameManager.terrainGenerator.chunkCreatedCount} timeCreateChunk:{gameManager.terrainGenerator.timeAverageCreate:F2} ms";

            directionButton.transform.localRotation = Quaternion.Euler(0f, 0f, -goalHandler.goalAngle);

            //            scoreText.text = $"Level: {gameManager.currentLevelNumber} Score:{score:N0} Bonus: {scoreManager.ScoreBonus+(int)scoreManager.bonusInProgress} Game Score:{scoreManager.ScoreOverall:N0}";
            if (gameManager.gameRunning)
            {
                float score = scoreManager.CalculateScoreGoal(gameManager.MusicPercentage, gameManager.GoalPercentage);
                itemLevel.SetValue(gameManager.currentLevelNumber.ToString());
                itemScore.SetValue(score.ToString());
                itemBonus.SetValue((scoreManager.ScoreBonus + (int)scoreManager.bonusInProgress).ToString());
                if (scoreManager.startBonus)
                    itemBonus.SetColor(Utilities.ColorWarning);
                else
                    itemBonus.ResetColor();
            }
            else
            {
                itemLevel.SetValue("");
                itemScore.SetValue("");
                itemBonus.SetValue("");
            }

            //    Color targetColor = scoreText.color; // couleur par défaut
            //    if (goalHandler.goalReached)
            //        targetColor = Utilities.ColorGreen;
            //    else
            //    {
            //        targetColor = scoreManager.CalculateColor();
            //    }

            //    if (currentTextColor != targetColor)
            //    {
            //        currentTextColor = targetColor;
            //        scoreText.color = currentTextColor;
            //    }
            //}

        }
    }
}