using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class SplashScreen : PanelDisplay
    {
        public Button help;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            help.onClick.AddListener(() => Application.OpenURL("https://paxstellar.fr/"));
            base.Start();
        }
    }
}