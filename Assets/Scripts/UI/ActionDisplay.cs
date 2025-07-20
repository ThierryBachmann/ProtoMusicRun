using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Collections;

public class ActionDisplay : PanelDisplay
{
    public GameManager gameManager;
    public Button rerunButton, continueButton, stopButton;

    public new void Awake()
    {
        base.Awake();
    }

    public new void Start()
    {
        rerunButton.onClick.AddListener(OnRerun);
        continueButton.onClick.AddListener(OnContinue);
        stopButton.onClick.AddListener(OnStop);
        base.Start();
    }

    void OnRerun()
    {
        Debug.Log("Re-run game");
        gameManager.RestartGame();
        Hide();
    }

    void OnContinue()
    {
        Debug.Log("Continue game");
        gameManager.NextLevel();
        Hide();
    }

    void OnStop()
    {
        Debug.Log("Stop game");
        gameManager.Stop();
        Hide();
    }
}
