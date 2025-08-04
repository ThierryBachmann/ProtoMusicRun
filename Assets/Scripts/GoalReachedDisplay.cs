using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace MusicRun
{
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


        private GameManager gameManager;
        private GoalHandler goalHandler;
        private PlayerController player;

        public void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
            goalHandler = gameManager.goalHandler;
            goalHandler.OnLevelCompleted += OnLevelCompleted;
        }

        public void Start()
        {
            NewLevel();
        }

        public void NewLevel()
        {
            Vector3 pos = panel.position;
            pos.y = startY;
            panel.position = pos;
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
            // Reminder,
            // for child GameObjects, the "Position" field in the Unity Inspector shows the value of transform.localPosition, not transform.position.
            // But World position (absolute in the scene) by script.
            Vector3 pos = panel.position;
            //Debug.Log($"RiseCoroutine {pos}");
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT = riseCurve.Evaluate(t);

                pos.y = startY + (endY - startY) * easedT;
                panel.position = pos;

                elapsed += Time.deltaTime;
                yield return null;
            }

            pos.y = endY;
            panel.position = pos;
        }
    }
}