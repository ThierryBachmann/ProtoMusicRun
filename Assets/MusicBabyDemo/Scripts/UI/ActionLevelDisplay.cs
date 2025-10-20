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
        public SwitchButton pauseButton; // see class SwitchButton which change background color
        public SettingScreen settingScreen;

        public new void Awake()
        {
            base.Awake();
            stopButton.gameObject.SetActive(false);
            pauseButton.OnValueChanged += (pause) =>
            {
                stopButton.gameObject.SetActive(pause);
                settingButton.gameObject.SetActive(pause);
                gameManager.OnSwitchPause(pause);
            };

            stopButton.onClick.AddListener(() => gameManager.StopGame());
            settingButton.onClick.AddListener(() => settingScreen.Show());
        }


        public new void Start()
        {
            base.Start();
        }

        /// <summary>
        /// Activates or deactivates the pause state by toggling the visibility and state of related UI elements.
        /// Call from gameManager.OnSwitchPause
        /// </summary>
        /// <param name="activate">A value indicating whether to activate the pause state.  <see langword="true"/> to activate pause; <see
        /// langword="false"/> to deactivate it.</param>
        public void ActivatePause(bool activate)
        {
            pauseButton.SetState(activate);
            stopButton.gameObject.SetActive(activate);
            settingButton.gameObject.SetActive(activate);
        }
    }
}