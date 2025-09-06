using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class ActionLevelDisplay : PanelDisplay
    {
        public HoldButton leftButton;
        public HoldButton rightButton;
        public HoldButton jumpButton;
        public SwitchButton pauseButton;
        public Button stopButton;

        public new void Awake()
        {
            base.Awake();
            stopButton.gameObject.SetActive(false);
            pauseButton.OnValueChanged += pauseChange;
            stopButton.onClick.AddListener(() => gameManager.StopGame());
        }

        public void ActivatePause(bool activate)
        {
            pauseButton.SetState(activate);
        }

        private void pauseChange(bool pause)
        {
            stopButton.gameObject.SetActive(pause);
            gameManager.OnSwitchPause(pause);
        }

        public new void Start()
        {
            base.Start();
        }
    }
}