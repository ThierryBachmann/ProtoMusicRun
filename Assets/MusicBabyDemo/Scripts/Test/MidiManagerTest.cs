using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static MusicRun.GameManager;

namespace MusicRun
{
    public class MidiManagerTest : MonoBehaviour
    {
        public int loopsToGoal = 2;
        public int countLoop = 0;
        public float progress;
        public float Progress
        {
            get
            {
                float progress;
                if (countLoop > loopsToGoal)
                    // If loop count exceeded goal loops, consider playback complete.
                    progress = 100f;
                else
                    progress = (float)(midiPlayer.MPTK_TickCurrent + ((countLoop - 1) * midiPlayer.MPTK_TickLastNote)) /
                        (float)(midiPlayer.MPTK_TickLastNote * loopsToGoal) * 100f;
                //Debug.Log($"MidiManager - Progress {progress} % MPTK_TickCurrent:{midiPlayer.MPTK_TickCurrent} MPTK_TickLastNote:{midiPlayer.MPTK_TickLastNote} {countLoop}/{gameManager.terrainGenerator.CurrentLevel.LoopsToGoal}");
                return progress;
            }
        }


        /// <summary>
        /// Reference to the MidiFilePlayer prefab instance used to play MIDI files.
        /// </summary>
        public MidiFilePlayer midiPlayer;


        /// <summary>
        /// Initialize references and configure the MidiFilePlayer.
        /// Called when the object is instantiated.
        /// </summary>
        void Awake()
        {
            Debug.Log("MidiManager - Awake");

            // Some MIDIs include silence before the first note or extra time after the last note.
            // These settings avoid that by starting playback at the first note and stopping at the last note.
            midiPlayer.MPTK_StartPlayAtFirstNote = true;
            midiPlayer.MPTK_StopPlayOnLastNote = true;

            //// When the MIDI reaches the end, we do not auto-restart by default.
            //midiPlayer.MPTK_MidiAutoRestart = true;

            // Continue playback even if the AudioListener moves too far from the AudioSource.
            // The volume may become zero due to attenuation, but the MIDI sequencer will keep running.
            // This behavior is required because the listener is attached to the player and the MidiPlayer's AudioSource
            // is placed at the goal. See UpdateMaxDistanceMPTK for per-level distance configuration.
            midiPlayer.MPTK_PauseOnMaxDistance = false;
            midiPlayer.MPTK_MaxDistance = 0;

            // Register event listeners for MIDI playback events.
            midiPlayer.OnEventStartPlayMidi.RemoveListener(OnStartMidi);
            midiPlayer.OnEventStartPlayMidi.AddListener(OnStartMidi);

            //midiPlayer.OnEventNotesMidi.RemoveListener(OnNotesMidi);
            //midiPlayer.OnEventNotesMidi.AddListener(OnNotesMidi);

            midiPlayer.OnEventEndPlayMidi.RemoveListener(OnEndMidi);
            midiPlayer.OnEventEndPlayMidi.AddListener(OnEndMidi);
        }

        private void OnStartMidi(string name)
        {
            countLoop++;
            Debug.Log($"MidiManager - OnEventStartPlayMidi '{name}' countLoop:{countLoop}");
        }

        private void OnNotesMidi(List<MPTKEvent> midiEvents)
        {
            // MIDI events callback (notes are being delivered). Kept for debugging or future handling.
            Debug.Log($"MidiManager Notes: {midiEvents.Count}");
        }

        private void OnEndMidi(string name, EventEndMidiEnum eventEndMidiEnum)
        {
            Debug.Log($"MidiManager - OnEventEndPlayMidi '{name}' endMidi:'{eventEndMidiEnum}'");
            // Additional logic can be triggered here when a MIDI finishes playing.
        }

        private void OnDestroy()
        {
            if (midiPlayer != null)
                midiPlayer.OnEventStartPlayMidi.RemoveListener(OnStartMidi);
        }

        void Start()
        {
            // Intentionally left empty: initialization is handled in Awake and by event callbacks.
        }

        public void Play()
        {
            midiPlayer.MPTK_Play();
        }

        public void Stop()
        { 
            midiPlayer.MPTK_Stop();
        }

        public void LoadMIDI(int index)
        {
            if (midiPlayer != null)
            {
                Debug.Log($"MidiPlayer - Load MIDI index {index}");

                countLoop = 0;
                // Ensure the player is stopped before loading a new file.
                //midiPlayer.MPTK_ModeStopVoice = MidiFilePlayer.ModeStopPlay.StopNoWaiting;
                midiPlayer.MPTK_MidiAutoRestart = false;
                midiPlayer.MPTK_Stop();

                //DateTime startTime = DateTime.Now;
                //float wait = 1000f;
                //if (midiPlayer.playerMidiHandle != null)
                //    while (midiPlayer.playerMidiHandle.IsRunning)
                //    {
                //        //midiPlayer.MPTK_Stop();
                //        //Routine.KillCoroutines(midiPlayer.playerMidiHandle);
                //        double waitingFor = (DateTime.Now - startTime).TotalMilliseconds;
                //        Debug.Log($"IsRunning:{midiPlayer.playerMidiHandle.IsRunning} {midiPlayer.playerMidiHandle.IsAliveAndPaused} {midiPlayer.playerMidiHandle.IsValid} {waitingFor:F1}");
                //        if (waitingFor > wait)
                //            break;
                //    }

                // Select and load the MIDI file but not play it.
                midiPlayer.MPTK_MidiIndex = index;
                midiPlayer.MPTK_Load();
            }
        }

        /// <summary>
        /// Play Load already loaded. OnEventStartPlayMidi will be trigger.
        /// </summary>
        /// <param name="index">Index of the MIDI file in the Midi DB.</param>
        public void PlayMIDI()
        {
            Debug.Log($"MidiManager - Play MIDI already loaded {midiPlayer.MPTK_MidiIndex}");

            // When game pause has been activated, the MIDI is paused and will not play.
            // Useless to call MPTK_UnPause from v2.17.1 - midiPlayer.MPTK_UnPause();
            // Play the already-loaded MIDI (avoid reloading).
            midiPlayer.MPTK_MidiAutoRestart = true;
            midiPlayer.MPTK_Play(alreadyLoaded: true);
        }

        void Update()
        {
            progress = Progress;
            if (progress >= 100f)
            {
                // GameManager .OnLevelCompleted() will decide if the level is failed or not. 
                //Debug.Log($"MidiManager - Raise OnMusicEnded countLoop: {countLoop} /  {loopsToGoal}");
            }
        }
    }
}