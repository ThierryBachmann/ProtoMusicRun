using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// icon from https://www.streamlinehq.com/icons/sharp-gradient
// 
namespace MusicRun
{

    public class ActionDisplay : PanelDisplay
    {
        public Button rerunButton, continueButton, stopButton, leaderBoardButton, restartLevel, helper;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            rerunButton.onClick.AddListener(() => gameManager.RestartGame());
            restartLevel.onClick.AddListener(() => gameManager.RestartLevel());
            continueButton.onClick.AddListener(() => gameManager.NextLevel());
            stopButton.onClick.AddListener(() => gameManager.StopGame());
            leaderBoardButton.onClick.AddListener(() => gameManager.LeaderboardSwitchDisplay());
            helper.onClick.AddListener(() => gameManager.SplashScreenDisplay());
            base.Start();
        }

        public void SelectActionsToShow()
        {
            if (gameManager.gameRunning && !gameManager.levelRunning)
            {
                // Waiting to start a level
                rerunButton.gameObject.SetActive(false);
                restartLevel.gameObject.SetActive(true);
                continueButton.gameObject.SetActive(true);
                stopButton.gameObject.SetActive(true);
            }
            if (!gameManager.gameRunning)
            {
                // Waiting to start a game
                rerunButton.gameObject.SetActive(true);
                restartLevel.gameObject.SetActive(false);
                continueButton.gameObject.SetActive(false);
                stopButton.gameObject.SetActive(false);
            }
        }
    }
}