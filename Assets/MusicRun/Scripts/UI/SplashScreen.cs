using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class SplashScreen : PanelDisplay
    {
        public Button helpGame;
        public Button settingGame;
        public SplashScreen splashScreen;
        public HelperScreen helperScreen;
        public SettingScreen settingScreen;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            helpGame.onClick.AddListener(() =>
            {
                splashScreen.Hide();
                helperScreen.Show();
            });

            settingGame.onClick.AddListener(() =>
            {
                splashScreen.Hide();
                settingScreen.Show();
            });


            base.Start();
        }
    }
}