using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace MusicRun
{
    public class GoalReachedDisplay : MonoBehaviour
    {
       public ScreenDisplay display;
         
        //public TMP_Text bestScoreText;
        public TMP_Text midiInfoDisplayed;

        private GameManager gameManager;

        public void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
        }

        public void Start()
        {
            NewLevel();
        }

        public void NewLevel()
        {
            display.ResetPosition();
        }

        public void LevelCompleted(bool failed)
        {
            UpdateText();
            display.ShowPanel();
        }

        private void UpdateText()
        {
            //// "   9999         9999            999"
            //// "  9999       9999         9999
            //bestScoreText.text = $" {player.playerLastScore,4}       {player.playerBestScore,4}            {player.playerPosition,4}";
            string midiInfo = $"{gameManager.midiManager.midiPlayer?.MPTK_MidiName}";
            //if (gameManager.midiManager.midiPlayer.MPTK_MidiLoaded != null)
            //{
            //    if (!string.IsNullOrEmpty(gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.TrackInstrumentName))
            //        midiInfo += "\n" + gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.TrackInstrumentName;
            //    if (!string.IsNullOrEmpty(gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.Copyright))
            //        midiInfo += "\n" + gameManager.midiManager.midiPlayer.MPTK_MidiLoaded.Copyright;
            //    // SequenceTrackName ProgramName    TrackInstrumentName
            //}
            midiInfoDisplayed.text = midiInfo;
        }
    }
}