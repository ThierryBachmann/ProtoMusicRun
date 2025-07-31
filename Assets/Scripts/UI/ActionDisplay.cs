using UnityEngine;
using UnityEngine.UI;

namespace MusicRun
{

    public class ActionDisplay : PanelDisplay
    {
        public Button rerunButton, continueButton, stopButton, leaderBoardButton;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            rerunButton.onClick.AddListener(() => gameManager.RestartGame());
            continueButton.onClick.AddListener(() => gameManager.NextLevel());
            stopButton.onClick.AddListener(() => gameManager.StopGame());
            leaderBoardButton.onClick.AddListener(() => gameManager.LeaderboardSwitchDisplay());
            base.Start();
        }
    }
}