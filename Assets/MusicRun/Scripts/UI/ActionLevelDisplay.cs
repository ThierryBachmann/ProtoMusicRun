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
        public Button stopButton;
        public Button settingButton;
        public SwitchButton pauseButton;
        public SettingScreen settingScreen;

        public new void Awake()
        {
            base.Awake();
            stopButton.gameObject.SetActive(false);
            pauseButton.OnValueChanged += pauseChange;
            stopButton.onClick.AddListener(() => gameManager.StopGame());
            settingButton.onClick.AddListener(() => settingScreen.Show());
        }

        public void ActivatePause(bool activate)
        {
            pauseButton.SetState(activate);
            stopButton.gameObject.SetActive(activate);
            settingButton.gameObject.SetActive(activate);
        }

        private void pauseChange(bool pause)
        {
            stopButton.gameObject.SetActive(pause);
            settingButton.gameObject.SetActive(pause);
            gameManager.OnSwitchPause(pause);
        }

        public new void Start()
        {
            base.Start();
        }
    }
}