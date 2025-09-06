using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MusicRun
{

    public class SettingScreen : PanelDisplay
    {
        public Toggle toggleLiteMode;

        public Action OnSettingChange;

        public new void Awake()
        {
            base.Awake();
        }

        public new void Start()
        {
            toggleLiteMode.isOn = gameManager.liteMode;
            toggleLiteMode.onValueChanged.AddListener((val) => 
            { 
                gameManager.liteMode = toggleLiteMode.isOn;
                OnSettingChange.Invoke();
            });
            base.Start();
        }
    }
}