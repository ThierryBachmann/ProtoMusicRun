using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;

public class GoalReachedDisplay : MonoBehaviour
{

    [Header("Main UI")]
    public GameObject leaderboardPanel;
    public TMP_Text bestScoreText;
    public TMP_Text midiInfoDisplayed;


    [Header("Reference")]
    public GameManager gameManager;
    public GoalHandler goalHandler;
    public PlayerController player;

    public void Awake()
    {
        // Subscribe to events
        goalHandler.OnLevelCompleted += OnLevelCompleted;
    }

    public void Start()
    {
    }

    private void OnLevelCompleted(bool success)
    {
        bestScoreText.text = $"Your Score {player.playerLastScore} - Your Best Score: {player.playerBestScore} - Your Position: {player.playerPosition}";
        midiInfoDisplayed.text = $"{gameManager.midiPlayer?.MPTK_MidiLoaded?.TrackInstrumentName}";
    }
}
