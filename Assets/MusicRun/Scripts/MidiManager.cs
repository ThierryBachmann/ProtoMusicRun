using MidiPlayerTK;
using System;
using System.Collections;
using UnityEngine;

namespace MusicRun
{
    public class MidiManager : MonoBehaviour
    {
        public MidiFilePlayer midiPlayer;
        public int[] channelPlayed = new int[16]; // Array to track which channels are currently playing
        public float Progress { get { return (float)midiPlayer.MPTK_TickCurrent / (float)midiPlayer.MPTK_TickLastNote * 100f; } }


        private float previousSpeed = -1;
        private GameManager gameManager;
        private PlayerController player;
        private GoalHandler goalHandler;
        private float savedVolume;
        private bool mute = false;

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
            goalHandler = gameManager.goalHandler;
            midiPlayer.MPTK_StartPlayAtFirstNote = true;
            midiPlayer.MPTK_MidiAutoRestart = false;
            midiPlayer.OnEventStartPlayMidi.AddListener((name) =>
            {
                // Start of the MIDI playback has been triggered.
                Debug.Log($"MidiPlayer - Play MIDI '{name}' {goalHandler.distanceAtStart}");
                // Reset some MIDI properties which can be done only when MIDI playback is started.
                Reset();
                StartCoroutine(UpdateMaxDistanceMPTK());
                midiPlayer.MPTK_Transpose = 0;
                midiPlayer.MPTK_MidiAutoRestart = false;
                Array.Clear(channelPlayed, 0, 16);
            });

            midiPlayer.OnEventNotesMidi.AddListener((midiEvents) =>
            {
                // Handle MIDI events if needed
                // Debug.Log($"MidiPlayer Notes: {midiEvents.Count}");
                foreach (var midiEvent in midiEvents)
                    channelPlayed[midiEvent.Channel]++;
            });

            midiPlayer.OnEventEndPlayMidi.AddListener((name, endMidi) =>
            {
                Debug.Log($"MidiPlayer - End MIDI '{name}' '{endMidi}' {goalHandler.distanceAtStart}");
            });
        }

        void Start()
        {

        }

        private IEnumerator UpdateMaxDistanceMPTK()
        {
            // Wait for the goalHandler to have a valid distanceAtStart.
            while (goalHandler.distanceAtStart < 0)
                yield return new WaitForSeconds(0.1f);
            // Volume attenuation according the distance between the player and the goal.
            // When the player is at the start, the volume is 5% of the volume max at the goal.
            midiPlayer.MPTK_MaxDistance = goalHandler.distanceAtStart * 1.05f;
            Debug.Log($"MidiPlayer - MaxDistance set {midiPlayer.MPTK_MaxDistance}");
        }

        public void Reset()
        {
            previousSpeed = -1;
        }
        public void Default()
        {
            previousSpeed = -1;
        }

        public void StartPlayMIDI(int index)
        {
            midiPlayer.MPTK_MidiIndex = index;
            if (midiPlayer != null)
            {
                midiPlayer.MPTK_Stop();
                midiPlayer.MPTK_Play();
            }
        }

        public void SoundOnOff()
        {
            if (mute)
                midiPlayer.MPTK_Volume = savedVolume;
            else
            {
                savedVolume = midiPlayer.MPTK_Volume;
                midiPlayer.MPTK_Volume = 0;
            }
        }
        public void Pause()
        {
            midiPlayer.MPTK_Pause();
        }
        public void UnPause()
        {
            midiPlayer.MPTK_UnPause();
        }

        public void ApplyPitch(float pitchFactor=0.99f, float durationMilli=2000f)
        {
            StartCoroutine(PitchRoutine(pitchFactor , durationMilli));
        }

        private IEnumerator PitchRoutine(float pitchFactor, float durationMilli)
        {
            float duration = (durationMilli / 1000f) / 10f;
            Debug.Log($"PitchRoutine {pitchFactor} {duration} * 10 sec.");
            for (int i = 0; i < 10; i++)
            {
                for (int v = 0; v < midiPlayer.ActiveVoices.Count; v++)
                {
                    fluid_voice voice = midiPlayer.ActiveVoices[v];
                    yield return null;
                    //Debug.Log(voice.VoiceAudio.name);
                    // When webplayer is enabled, all notes are played with independent Audiosource
                    voice.VoiceAudio.Audiosource.pitch *= pitchFactor;
                }
                yield return Routine.WaitForSeconds(durationMilli);
            }
        }

        void Update()
        {
            float speedClamp = 1f;

            // Calculate music speed from the player speed
            if (gameManager.levelRunning)
            {
                Level current = gameManager.terrainGenerator.CurrentLevel;
                float speedMusic = player.speedMultiplier * current.RatioSpeedMusic;
                speedClamp = Mathf.Clamp(speedMusic, current.MinSpeedMusic, current.MaxSpeedMusic);
            }

            // Avoid changing speed at each frame
            if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 0.1f)
            {
                Debug.Log($"MidiPlayer - player.speedMultiplier: {player.speedMultiplier} music speed {speedClamp}");
                midiPlayer.MPTK_Speed = speedClamp;
                previousSpeed = speedClamp;
            }
        }
    }
}