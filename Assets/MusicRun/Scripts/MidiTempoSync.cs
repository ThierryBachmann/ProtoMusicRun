using MidiPlayerTK;
using System;
using System.Collections;
using UnityEngine;

namespace MusicRun
{
    public class MidiTempoSync : MonoBehaviour
    {
        public MidiFilePlayer midiPlayer;
        public int[] channelPlayed = new int[16]; // Array to track which channels are currently playing
        public float Progress { get { return (float)midiPlayer.MPTK_TickCurrent / (float)midiPlayer.MPTK_TickLastNote * 100f; } }

        // If true, speed will change with tempo changes. Disabled by default (finally, not useful)
        private bool SpeedAsTempoChange = false;
        private float RatioTempoMusic = 0.4f;
        private float MinTempoMusic = 50f;
        private float MaxTempoMusic = 300f;

        private float previousSpeed = -1;
        private GameManager gameManager;
        private PlayerController player;
        public GoalHandler goalHandler;
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
        }

        void Start()
        {
  
        }

        private IEnumerator UpdateMaxDistanceMPTK()
        {
            // Wait for the goalHandler to have a valid distanceAtStart.
            while (goalHandler.distanceAtStart < 0)
                yield return new WaitForSeconds(0.1f);
            // Attenuation of volume with the distance from the player and the goal.
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


        void Update()
        {
            if (SpeedAsTempoChange)
            {
                float speed = 50 + player.speedMultiplier * RatioTempoMusic;
                float speedClamp = Mathf.Clamp(speed, MinTempoMusic, MaxTempoMusic);
                if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 2f)
                {
                    Debug.Log($"MidiPlayer - player.speedMultiplier: {player.speedMultiplier} music tempo {speedClamp}");
                    midiPlayer.MPTK_Tempo = speedClamp;
                    previousSpeed = speedClamp;
                }
            }
            else
            {
                float speedClamp = 1f;
                if (gameManager.levelRunning)
                {
                    Level current = gameManager.terrainGenerator.CurrentLevel;
                    float speed = player.speedMultiplier * current.RatioSpeedMusic;
                    speedClamp = Mathf.Clamp(speed, current.MinSpeedMusic, current.MaxSpeedMusic);
                }
                if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 0.1f)
                {
                    //Debug.Log($"player.speedMultiplier: {player.speedMultiplier} music speed {speedClamp}");
                    midiPlayer.MPTK_Speed = speedClamp;
                    previousSpeed = speedClamp;
                }
            }
        }
    }
}