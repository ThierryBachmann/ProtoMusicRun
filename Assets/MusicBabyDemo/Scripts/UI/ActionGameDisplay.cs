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
            startGameButton.onClick.AddListener(() => gameManager.StartGame());
            retryLevelButton.onClick.AddListener(() => gameManager.RetryLevel());
            nextLevelButton.onClick.AddListener(() => gameManager.NextLevel());
            stopButton.onClick.AddListener(() => gameManager.StopGame());
            leaderBoardButton.onClick.AddListener(() => gameManager.LeaderboardSwitchDisplay());
            helper.onClick.AddListener(() => gameManager.SplashScreenDisplay());

            // Game logic from the gamepad
            gameManager.touchEnabler.controls.Gameplay.Start.performed += (InputAction.CallbackContext context) =>
            {
                if (!gameManager.gameRunning)
                    gameManager.StartGame();
                else if (gameManager.levelPaused)
                {
                    if (gameManager.levelFailed)
                        gameManager.RetryLevel();
                    else
                        gameManager.NextLevel();

                }
                else if (!gameManager.levelRunning)
                {
                    gameManager.NextLevel();
                }
            };

            base.Start();
        }

        public void SelectActionsToShow()
        {
            if (gameManager.gameRunning && !gameManager.levelRunning)
            {
                Debug.Log("Waiting to start a level when game is running");
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

            if (!gameManager.gameRunning)
            {
                Debug.Log("Waiting to start a game");
                startGameButton.gameObject.SetActive(true);
                retryLevelButton.gameObject.SetActive(false);
                nextLevelButton.gameObject.SetActive(false);
                stopButton.gameObject.SetActive(false);
            }
        }
    }
}