using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MusicRun
{
    public class HeaderDisplay : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI liteModeActivated;
        public TitleItem itemLevel;
        public TitleItem itemScore;
        public TitleItem itemBonus;
        public TitleItem itemInstrument;
        public TextMeshProUGUI infoText;
        public Button quitButton;
        public Button directionButton;

        private Color currentTextColor;
        private GameManager gameManager;
        private ScoreManager scoreManager;
        private BonusManager bonusManager;
        private PlayerController player;
        private GoalHandler goalHandler;

        private void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            scoreManager = gameManager.scoreManager;
            bonusManager = gameManager.bonusManager;
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

            if (!gameManager.infoDebug)
                infoText.gameObject.SetActive(false);

            liteModeActivated.gameObject.SetActive(false);

        }

        public void LiteModeDisplay(bool display)
        {
            liteModeActivated.gameObject.SetActive(display);
        }

        public void SetTitle()
        {
            titleText.text = $"Music Run - {player.playerName}";
        }


        void Update()
        {

            //scoreText.text = $"{gameManager.leaderboard.GetPlayerName()} Level: {gameManager.currentLevelNumber} Score:{scoreManager.ScoreOverall:N0} Bonus: {scoreManager.ScoreBonus} Speed:{player.GetSpeed():N1}";

            //infoText.text = $"Debug index level:{gameManager.currentLevelIndex} dir:{goalHandler.goalDirection:F2} angle:{goalHandler.goalAngle:F2}";


            if (gameManager.infoDebug)
            {
                //infoText.text = $"Debug index level:{gameManager.currentLevelIndex} chunkCreatedCount:{gameManager.terrainGenerator.chunkCreatedCount} timeCreateChunk:{gameManager.terrainGenerator.timeAverageCreate:F2} ms";
                infoText.text = $"Debug index level:{gameManager.levelIndex} Speed:{player.Speed:F1} Angle:{player.targetAngle:F0}/{player.currentAngle:F0} chunkCreatedCount:{gameManager.terrainGenerator.chunkCreatedCount} FPS:{gameManager.FramePerSecond} ";
            }

            directionButton.transform.localRotation = Quaternion.Euler(0f, 0f, -goalHandler.goalAngle);

            //            scoreText.text = $"Level: {gameManager.currentLevelNumber} Score:{score:N0} Bonus: {scoreManager.ScoreBonus+(int)scoreManager.bonusInProgress} Game Score:{scoreManager.ScoreOverall:N0}";
            if (gameManager.gameRunning)
            {
                float score = scoreManager.CalculateScoreGoal(gameManager.MusicPercentage, gameManager.GoalPercentage);
                itemLevel.SetValue(gameManager.levelNumber.ToString());
                itemScore.SetValue(score.ToString());
                itemBonus.SetValue((scoreManager.ScoreBonus + (int)bonusManager.bonusInProgress).ToString());
                if (bonusManager.startBonus)
                    if (bonusManager.valueBonus > 0)
                        itemBonus.SetColor(Utilities.ColorGreen);
                    else
                        itemBonus.SetColor(Utilities.ColorWarning);
                else
                    itemBonus.ResetColor();
                itemInstrument.SetValue($"{gameManager.midiManager.InstrumentRestored} / {gameManager.midiManager.InstrumentFound}");
                if (gameManager.midiManager.InstrumentRestored >= gameManager.midiManager.InstrumentFound)
                    itemInstrument.SetColor(Utilities.ColorGreen);
            }
            else
            {
                itemLevel.SetValue("");
                itemScore.SetValue("");
                itemBonus.SetValue("");
                itemInstrument.SetValue("");
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