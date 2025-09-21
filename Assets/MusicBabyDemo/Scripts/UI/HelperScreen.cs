using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class HelperScreen : PanelDisplay
    {
        public Button unity;
        public Button help;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            unity.onClick.AddListener(() => Application.OpenURL("https://assetstore.unity.com/packages/tools/audio/maestro-midi-player-tool-kit-free-107994"));
            help.onClick.AddListener(() => Application.OpenURL("https://paxstellar.fr/"));
            base.Start();
        }
    }
}