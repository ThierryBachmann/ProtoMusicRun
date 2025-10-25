using MidiPlayerTK;
using MPTK.NAudio.Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MusicRun
{
    public class MidiManager : MonoBehaviour
    {
        public class ChannelInstrument
        {
            public int Channel;
            public int Preset;
            public bool Restored;
            public MPTKEvent MidiEventChanged;
        }
        public MidiFilePlayer midiPlayer;
        public Dictionary<int, ChannelInstrument> ChannelPlayed;
        public int InstrumentFound;
        public int InstrumentRestored;

        /// <summary>
        /// Calculate playing progression in percentage. 
        /// </summary>
        public float Progress
        {
            get
            {
                //Debug.Log($"Tick First Note: {midiPlayer.MPTK_TickFirstNote} Tick Last Note: {midiPlayer.MPTK_TickLastNote} Tick Current: {midiPlayer.MPTK_TickCurrent}");
                if (countLoop > gameManager.terrainGenerator.CurrentLevel.LoopsToGoal)
                    return 100f;
                else
                    return
                        (float)(midiPlayer.MPTK_TickCurrent + ((countLoop - 1) * midiPlayer.MPTK_TickLastNote)) /
                        (float)(midiPlayer.MPTK_TickLastNote * gameManager.terrainGenerator.CurrentLevel.LoopsToGoal) * 100f;
            }
        }


        private float previousSpeed = -1;
        private GameManager gameManager;
        private PlayerController player;
        private GoalHandler goalHandler;
        private float savedVolume;
        private bool mute = false;
        public int countLoop = 0;

        /// <summary>
        /// Instantiated at each new level, perhaps not the best approach ...
        /// </summary>
        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
            goalHandler = gameManager.goalHandler;

            // Some MIDIs have delay before starting playing music and extra time at the last note.
            // Avoid this with this configuration.
            midiPlayer.MPTK_StartPlayAtFirstNote = true;
            midiPlayer.MPTK_StopPlayOnLastNote = true;

            // When the MIDI reach the end, it's a signal to stop the level. 
            midiPlayer.MPTK_MidiAutoRestart = false;

            // Continue playing when the player (which holds the AudioListener) is to far to the AudioSource (holds by the MidiPlayer at the goal).
            // The volume sound will be zero but the MIDI sequencer is not pause. Maestro MPTK 2.16.1.
            // Note: distance is defined for each scene with MPTK_MaxDistance see UpdateMaxDistanceMPTK()
            midiPlayer.MPTK_PauseOnMaxDistance = false;
            midiPlayer.MPTK_MaxDistance = 0;

            midiPlayer.OnEventStartPlayMidi.AddListener((name) =>
            {
                // MIDI playback start triggered.
                Debug.Log($"MidiPlayer - Play MIDI '{name}' {goalHandler.distanceAtStart}");
                // Reset some MIDI properties which can be done only when MIDI playback is started.
                Reset();
                StartCoroutine(UpdateMaxDistanceMPTK());
                countLoop++;
                midiPlayer.MPTK_Transpose = 0;
                midiPlayer.MPTK_MidiAutoRestart = gameManager.terrainGenerator.CurrentLevel.LoopsToGoal == 1 ? false : true;
            });

            midiPlayer.OnEventNotesMidi.AddListener((midiEvents) =>
            {
                // Handle MIDI events if needed
                // Debug.Log($"MidiPlayer Notes: {midiEvents.Count}");
            });

            midiPlayer.OnEventEndPlayMidi.AddListener((name, endMidi) =>
            {
                Debug.Log($"MidiPlayer - End MIDI '{name}' '{endMidi}' {goalHandler.distanceAtStart}");
                // if (gameManager.terrainGenerator.CurrentLevel.LoopsToGoal > 0)
            });
        }

        void Start()
        {

        }

        /// <summary>
        /// The volume is directly linked to the distance between the player and the goal.
        /// Set at new level.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Where we start playing the MIDI
        /// </summary>
        /// <param name="index"></param>
        public void StartPlayMIDI(int index)
        {
            if (midiPlayer != null)
            {
                countLoop = 0;
                // Not necessary but in case of ...
                midiPlayer.MPTK_Stop();

                // Select and load the MIDI
                midiPlayer.MPTK_MidiIndex = index;
                midiPlayer.MPTK_Load();
                ClearMidiChannel();
                // PLay MIDI, avoid to reload it
                midiPlayer.MPTK_Play(alreadyLoaded: true);
            }
        }

        private void ClearMidiChannel()
        {
            // Search instrument played on each channel
            ChannelPlayed = new Dictionary<int, ChannelInstrument>();

            foreach (MPTKEvent midiEvent in midiPlayer.MPTK_MidiLoaded.MPTK_MidiEvents)
                if (midiEvent.Command == MPTKCommand.PatchChange)
                {
                    if (!ChannelPlayed.ContainsKey(midiEvent.Channel))
                    {
                        ChannelPlayed.Add(midiEvent.Channel, new ChannelInstrument()
                        {
                            Channel = midiEvent.Channel,
                            Preset = midiEvent.Value,
                            Restored = false,
                            MidiEventChanged = midiEvent
                        });
                        Debug.Log($"MidiPlayer - Found instrument: {midiEvent.Value} on channel {midiEvent.Channel} ");
                    }
                    else
                        Debug.Log($"MidiPlayer - Instrument already found on Channel {midiEvent.Channel} instrument: {midiEvent.Value}");
                    if (gameManager.terrainGenerator.CurrentLevel.SearchForInstrument)
                        midiEvent.Value = gameManager.terrainGenerator.CurrentLevel.SubstitutionInstrument;
                }
            InstrumentFound = ChannelPlayed.Count;
            InstrumentRestored = 0;
        }

        public void RestoreMidiChannel()
        {
            foreach (ChannelInstrument instrument in ChannelPlayed.Values)
                if (!instrument.Restored)
                {
                    midiPlayer.MPTK_Channels[instrument.Channel].PresetNum = instrument.Preset;
                    instrument.MidiEventChanged.Value = instrument.Preset;
                    instrument.Restored = true;
                    InstrumentRestored++;
                    Debug.Log($"MidiPlayer - Restore instrument {instrument.Preset} on channel {instrument.Channel} ");
                    break;
                }
        }

        /// <summary>
        /// When the sound is off, the MIDI continue playing
        /// </summary>
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

        public void TransposeSet(int transpose)
        {
            midiPlayer.MPTK_Transpose = transpose;
        }

        public void TransposeAdd(int transpose)
        {
            midiPlayer.MPTK_Transpose += transpose;
        }

        public void TransposeClear()
        {
            midiPlayer.MPTK_Transpose = 0;
        }

        /// <summary>
        /// Apply a pitch change on a channel for a duration. The pitch change is progressive along the duration.
        /// </summary>
        /// <param name="pitchTarget"></param>
        /// <param name="durationMilli"></param>
        public void ApplyPitchChannel(float pitchTarget = 0.5f, float durationMilli = 2000f)
        {
            if (pitchTarget < 0f || pitchTarget > 1f)
            {
                Debug.LogWarning($"PitchChannelRoutine - pitchFactor {pitchTarget} is incorrect, must be between 0 and 1");
                return;
            }
            if (durationMilli < 50f || durationMilli >= 100000f)
            {
                Debug.LogWarning($"PitchChannelRoutine - durationMilli {durationMilli} is incorrect, must be between 10f and 10000f");
                return;
            }
            StartCoroutine(PitchChannelRoutine(pitchTarget, durationMilli));
        }

        /// <summary>
        /// Change pitch (automatic return to center as a physical keyboard!)
        /// Gift from MPTK Pro! See MPTK_PlayPitchWheelChange.
        ///   0       the lowest bend positions(default is 2 semitones), 
        ///   0.5     centered value, the sounding notes aren't being transposed up or down,
        ///   1       highest pitch bend position (default is 2 semitones)
        /// </summary>
        /// <param name="pitchTarget"></param>
        /// <param name="durationMilli"></param>
        /// <returns></returns>
        private IEnumerator PitchChannelRoutine(float pitchTarget, float durationMilli)
        {
            //Debug.Log($"PitchChannelRoutine {pitchTarget} {durationMilli}");
            float pitch = 0.5f; // centered value
            float waitMillisecond = 100f; // Wait between each pitch change
            float deltaPitchMilli = (pitchTarget - 0.5f) / durationMilli; // Delta pitch between each pitch change
            DateTime stop = DateTime.Now.AddMilliseconds(durationMilli);
            DateTime now = DateTime.Now;

            while (now < stop)
            {
                // Useless, just for security
                if (deltaPitchMilli < 0 && pitch <= pitchTarget) break;
                if (deltaPitchMilli > 0 && pitch >= pitchTarget) break;

                pitch += ((float)(DateTime.Now - now).TotalMilliseconds) * deltaPitchMilli;
                //Debug.Log($"deltaPitchMilli: {deltaPitchMilli:F6} deltaTime: {(DateTime.Now - now).TotalMilliseconds:F6} pitch: {pitch:F6}");
                now = DateTime.Now;

                for (int channel = 0; channel < 16; channel++)
                {
                    MPTKEvent mptkEvent = new MPTKEvent()
                    {
                        Command = MPTKCommand.PitchWheelChange,
                        Channel = channel,
                        Value = (int)Mathf.Lerp(0f, 16383f, pitch),
                    };
                    midiPlayer.MPTK_PlayDirectEvent(mptkEvent);
                }
                yield return Routine.WaitForSeconds(waitMillisecond / 1000f);
            }

            // Restore original pitch by sending a MIDI command PitchWheelChange directly to the MIDI synthesizer
            for (int channel = 0; channel < 16; channel++)
            {
                MPTKEvent mptkEvent = new MPTKEvent()
                {
                    Command = MPTKCommand.PitchWheelChange,
                    Channel = channel,
                    Value = 8192
                };
                midiPlayer.MPTK_PlayDirectEvent(mptkEvent);
            }
        }

        public void ApplyPitchAudioSource(float pitchFactor = 0.99f, float durationMilli = 2000f)
        {
            if (pitchFactor < 0.2f || pitchFactor >= 2f)
            {
                Debug.LogWarning($"ApplyPitchAudioSource - pitchFactor {pitchFactor} is incorrect, must be between 0.2 and 2");
                return;
            }
            if (durationMilli < 10f || durationMilli >= 100000f)
            {
                Debug.LogWarning($"ApplyPitchAudioSource - durationMilli {durationMilli} is incorrect, must be between 10f and 10000f");
                return;
            }
            StartCoroutine(PitchAudioSourceRoutine(pitchFactor, durationMilli));
        }

        private IEnumerator PitchAudioSourceRoutine(float pitchFactor, float durationMilli)
        {
            float duration = (durationMilli / 1000f) / 10f;
            //Debug.Log($"PitchRoutine {pitchFactor} {duration} * 10 sec.");
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
            if (gameManager.levelRunning && !gameManager.levelPaused)
            {
                // Min and max music speed are defined by level
                TerrainLevel current = gameManager.terrainGenerator.CurrentLevel;
                float speedMusic = player.Speed * current.RatioSpeedMusic;
                speedClamp = Mathf.Clamp(speedMusic, current.MinSpeedMusic, current.MaxSpeedMusic);
            }

            // Avoid changing speed at each frame but every 100 ms
            if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 0.1f)
            {
                //Debug.Log($"MidiPlayer - player.speedMultiplier: {player.speedMultiplier} music speed {speedClamp}");
                midiPlayer.MPTK_Speed = speedClamp;
                previousSpeed = speedClamp;
            }
        }
    }
}