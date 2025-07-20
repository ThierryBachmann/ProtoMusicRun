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
    public Button rerunButton, continueButton, stopButton, leaderBoardButton;

    public new void Awake()
    {
        base.Awake();
    }

    public new void Start()
    {
        rerunButton.onClick.AddListener(() => gameManager.RestartGame());
        continueButton.onClick.AddListener(() => gameManager.NextLevel());
        stopButton.onClick.AddListener(() => gameManager.Stop());
        leaderBoardButton.onClick.AddListener(() => gameManager.LeaderboardSwitchDisplay());
        base.Start();
    }
}
