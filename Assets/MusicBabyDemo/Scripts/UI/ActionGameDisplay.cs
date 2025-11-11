using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// icon from https://www.streamlinehq.com/icons/sharp-gradient
// 
namespace MusicRun
{

    public class ActionGameDisplay : PanelDisplay
    {
        public Button startGameButton, nextLevelButton, stopButton, leaderBoardButton, retryLevelButton, helper;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            // Game logic from the button on screen
            startGameButton.onClick.AddListener(() => gameManager.GameStart());
            retryLevelButton.onClick.AddListener(() => gameManager.LevelRetry());
            nextLevelButton.onClick.AddListener(() => gameManager.LevelNext());
            stopButton.onClick.AddListener(() => gameManager.GameStop());
            leaderBoardButton.onClick.AddListener(() => gameManager.LeaderboardSwitchDisplay());
            helper.onClick.AddListener(() => gameManager.SplashScreenDisplay());

            base.Start();
        }

        public void SelectActionsToShow()
        {
            if (!gameManager.gameRunning)
            {
                Debug.Log("Waiting to start a new game");
                startGameButton.gameObject.SetActive(true);
                retryLevelButton.gameObject.SetActive(false);
                nextLevelButton.gameObject.SetActive(false);
                stopButton.gameObject.SetActive(false);
            }
            else if (!gameManager.levelRunning)
            {
                Debug.Log("Waiting to start a level");
                startGameButton.gameObject.SetActive(false);
                if (gameManager.levelFailed)
                {
                    retryLevelButton.gameObject.SetActive(true);
                    nextLevelButton.gameObject.SetActive(false);
                }
                else
                {
                    retryLevelButton.gameObject.SetActive(true);
                    nextLevelButton.gameObject.SetActive(true);
                }
                stopButton.gameObject.SetActive(true);
            }

        }
    }
}