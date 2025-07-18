using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Collections;

public class ActionDisplay : PanelDisplay
{
    public Button rerunButton, continueButton, stopButton;

    public new void Awake()
    {
        base.Awake();
    }

    public new void Start()
    {
        rerunButton.onClick.AddListener(OnRerun);
        base.Start();
    }

    void OnRerun()
    {
        Debug.Log("Re-run game");
        Hide();
    }

    void OnContinue()
    {
        Debug.Log("Continue game");
        Hide();
    }

    void OnStop()
    {
        Debug.Log("Stop game");
        Hide();
    }
}
