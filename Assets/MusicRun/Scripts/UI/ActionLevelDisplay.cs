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

        public new void Awake()
        {
            base.Awake();
            pauseButton.OnValueChanged += gameManager.OnSwitchPause;
        }

        public new void Start()
        {
            base.Start();
        }
    }
}