using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class SplashScreen : PanelDisplay
    {
        public Button unity;
        public Button mptk;
        public Button helpGame;
        public SplashScreen splashScreen;
        public HelperScreen helperScreen;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            unity.onClick.AddListener(() => Application.OpenURL("https://assetstore.unity.com/packages/tools/audio/maestro-midi-player-tool-kit-free-107994"));
            mptk.onClick.AddListener(() => Application.OpenURL("https://paxstellar.fr/"));
            helpGame.onClick.AddListener(() =>
            {
                splashScreen.Hide();
                helperScreen.Show();
            });

            base.Start();
        }
    }
}