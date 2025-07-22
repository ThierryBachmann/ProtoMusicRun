using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Collections;

public class GoalReachedDisplay : MonoBehaviour
{

    [Header("Main UI")]
    public Transform panel;
    public float duration = 1.5f;
    public float startY = -1.5f;
    public float endY = 0f;
    // Animation curve for a smooth motion (starts fast, ends slow)
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public TMP_Text bestScoreText;
    public TMP_Text midiInfoDisplayed;


    [Header("Reference")]
    public GameManager gameManager;
    public GoalHandler goalHandler;
    public PlayerController player;

    private Vector3 startPos;
    private Vector3 endPos;

    public void Awake()
    {
        goalHandler.OnLevelCompleted += OnLevelCompleted;
    }

    public void Start()
    {
        // Starting position below the ground
        startPos = new Vector3(panel.position.x, startY, panel.position.z);

        // Final visible position
        endPos = new Vector3(panel.position.x, endY, panel.position.z);

        // Set panel to starting position
        panel.position = startPos;
    }

    public void Reset()
    {
        panel.position = startPos; 
    }

    private void OnLevelCompleted(bool success)
    {
        UpdateText();
        ShowPanel();
    }

    private void UpdateText()
    {
        // "   9999         9999            999"
        // "  9999       9999         9999
        bestScoreText.text = $" {player.playerLastScore,4}       {player.playerBestScore,4}            {player.playerPosition,4}";
        string midiInfo = $"{gameManager.midiPlayer?.MPTK_MidiName}";
        if (gameManager.midiPlayer.MPTK_MidiLoaded != null)
        {
            if (!string.IsNullOrEmpty(gameManager.midiPlayer.MPTK_MidiLoaded.TrackInstrumentName))
                midiInfo += "\n" + gameManager.midiPlayer.MPTK_MidiLoaded.TrackInstrumentName;
            if (!string.IsNullOrEmpty(gameManager.midiPlayer.MPTK_MidiLoaded.Copyright))
                midiInfo += "\n" + gameManager.midiPlayer.MPTK_MidiLoaded.Copyright;
            // SequenceTrackName ProgramName    TrackInstrumentName
        }
        midiInfoDisplayed.text = midiInfo;
    }

    public void ShowPanel()
    {
        StartCoroutine(RiseCoroutine());
    }
    private IEnumerator RiseCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedT = riseCurve.Evaluate(t);
            panel.position = Vector3.Lerp(startPos, endPos, easedT);
            elapsed += Time.deltaTime;
            yield return null;
        }

        panel.position = endPos; 
    }
}
