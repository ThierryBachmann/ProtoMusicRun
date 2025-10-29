using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MusicRun
{
    public class MidiManager : MonoBehaviour
    {
        /// <summary>
        /// Information about a program (preset) change found in the loaded MIDI file.
        /// </summary>
        public class ChannelInstrument
        {
            /// <summary>Channel index (0-15) where the preset change was observed.</summary>
            public int Channel;
            /// <summary>Original preset (program) number found in the MIDI event.</summary>
            public int Preset;
            /// <summary>True when the original preset has been restored during gameplay.</summary>
            public bool Restored;
            /// <summary>Reference to the original MIDI PatchChange event so the value can be restored if needed.</summary>
            public MPTKEvent MidiEventChanged;
        }

        /// <summary>
        /// Reference to the MidiFilePlayer prefab instance used to play MIDI files.
        /// </summary>
        public MidiFilePlayer midiPlayer;

        /// <summary>
        /// Map of MIDI channels to detected instruments (preset/program changes).
        /// </summary>
        public Dictionary<int, ChannelInstrument> ChannelPlayed;

        /// <summary>Number of distinct instruments found in the currently loaded MIDI.</summary>
        public int InstrumentFound;
        /// <summary>Number of instruments restored so far (used when progressively restoring originals).</summary>
        public int InstrumentRestored;

        /// <summary>
        /// Playback progress expressed as a percentage (0..100).
        /// This takes into account looping: progress increases with loop count up to the level goal.
        /// </summary>
        public float Progress
        {
            get
            {
                // If loop count exceeded goal loops, consider playback complete.
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
        /// Initialize references and configure the MidiFilePlayer.
        /// Called when the object is instantiated.
        /// </summary>
        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
            goalHandler = gameManager.goalHandler;

            // Some MIDIs include silence before the first note or extra time after the last note.
            // These settings avoid that by starting playback at the first note and stopping at the last note.
            midiPlayer.MPTK_StartPlayAtFirstNote = true;
            midiPlayer.MPTK_StopPlayOnLastNote = true;

            // When the MIDI reaches the end, we do not auto-restart by default.
            midiPlayer.MPTK_MidiAutoRestart = false;

            // Continue playback even if the AudioListener moves too far from the AudioSource.
            // The volume may become zero due to attenuation, but the MIDI sequencer will keep running.
            // This behavior is required because the listener is attached to the player and the MidiPlayer's AudioSource
            // is placed at the goal. See UpdateMaxDistanceMPTK for per-level distance configuration.
            midiPlayer.MPTK_PauseOnMaxDistance = false;
            midiPlayer.MPTK_MaxDistance = 0;

            midiPlayer.OnEventStartPlayMidi.AddListener((name) =>
            {
                // MIDI playback started.
                Debug.Log($"MidiPlayer - Play MIDI '{name}' {goalHandler.distanceAtStart}");
                // Reset transient state that depends on playback start.
                Reset();
                StartCoroutine(UpdateMaxDistanceMPTK());
                countLoop++;
                midiPlayer.MPTK_Transpose = 0;
                // Enable auto-restart only if the level requires multiple loops to reach the goal.
                midiPlayer.MPTK_MidiAutoRestart = gameManager.terrainGenerator.CurrentLevel.LoopsToGoal == 1 ? false : true;
            });

            midiPlayer.OnEventNotesMidi.AddListener((midiEvents) =>
            {
                // MIDI events callback (notes are being delivered). Kept for debugging or future handling.
                // Debug.Log($"MidiPlayer Notes: {midiEvents.Count}");
            });

            midiPlayer.OnEventEndPlayMidi.AddListener((name, endMidi) =>
            {
                Debug.Log($"MidiPlayer - End MIDI '{name}' '{endMidi}' {goalHandler.distanceAtStart}");
                // Additional logic can be triggered here when a MIDI finishes playing.
            });
        }

        void Start()
        {
            // Intentionally left empty: initialization is handled in Awake and by event callbacks.
        }

        /// <summary>
        /// Set the MidiFilePlayer's maximum attenuation distance based on the level start distance.
        /// This method waits until the level has calculated a valid start distance.
        /// </summary>
        private IEnumerator UpdateMaxDistanceMPTK()
        {
            // Wait until the goal handler has a valid start distance.
            while (goalHandler.distanceAtStart < 0)
                yield return new WaitForSeconds(0.1f);

            // Set the max distance used for volume attenuation. When the player is at the start,
            // the volume at the listener will be slightly lower than at the goal.
            midiPlayer.MPTK_MaxDistance = goalHandler.distanceAtStart * 1.05f;
            Debug.Log($"MidiPlayer - MaxDistance set {midiPlayer.MPTK_MaxDistance}");
        }

        /// <summary>
        /// Reset transient state tracked by MidiManager (for example when playback restarts).
        /// </summary>
        public void Reset()
        {
            previousSpeed = -1;
        }

        /// <summary>
        /// Restore default transient state. Alias for Reset to express intent.
        /// </summary>
        public void Default()
        {
            previousSpeed = -1;
        }

        /// <summary>
        /// Load a MIDI from the Midi DB by index and build channel index.
        /// </summary>
        /// <param name="index">Index of the MIDI file in the Midi DB.</param>
        public void LoadMIDI(TerrainLevel terrainLevel)
        {
            if (midiPlayer != null)
            {
                countLoop = 0;
                // Ensure the player is stopped before loading a new file.
                midiPlayer.MPTK_Stop();

                // Select and load the MIDI file.
                midiPlayer.MPTK_MidiIndex = terrainLevel.indexMIDI;
                midiPlayer.MPTK_Load();
            }
        }

        /// <summary>
        /// PLay Load already loaded. OnEventStartPlayMidi will be trigger.
        /// </summary>
        /// <param name="index">Index of the MIDI file in the Midi DB.</param>
        public void PlayMIDI()
        {
            // Play the already-loaded MIDI (avoid reloading).
            midiPlayer.MPTK_Play(alreadyLoaded: true);
        }


        /// <summary>
        /// Clears and initializes the MIDI channel data by identifying instruments (program changes) used on each channel.
        /// </summary>
        /// <remarks>
        /// This method resets the <see cref="ChannelPlayed"/> dictionary and populates it with
        /// instruments found in the loaded MIDI events. It logs the instruments found on each channel and updates the
        /// MIDI event value if the level requests a substitution instrument.
        /// Only the first program change found in a channel is taken into account. The next will not be handled.
        /// So, the first program change found will be restoread when an instrument is found by the player. 
        /// </remarks>
        public void BuildMidiChannel(TerrainLevel terrainLevel)
        {
            // Build a map of instruments played on each channel.
            ChannelPlayed = new Dictionary<int, ChannelInstrument>();

            foreach (MPTKEvent midiEvent in midiPlayer.MPTK_MidiLoaded.MPTK_MidiEvents)
                if (midiEvent.Command == MPTKCommand.PatchChange)
                {
                    if (!ChannelPlayed.ContainsKey(midiEvent.Channel))
                    {
                        // Store instrument information and the original PatchChange MIDI event so we can restore it later
                        // if the MIDI loops without being reloaded.
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

                    // If the current level asks to "SearchForInstrument", override the preset to the substitution instrument.
                    if (terrainLevel.SearchForInstrument)
                        midiEvent.Value = terrainLevel.SubstitutionInstrument;
                }
            InstrumentFound = ChannelPlayed.Count;
            InstrumentRestored = 0;
        }

        /// <summary>
        /// Restore the next (first non-restored) MIDI channel to its original instrument preset.
        /// </summary>
        /// <remarks>
        /// Iterates through detected instruments and restores the first one that has not yet been restored.
        /// After restoration, the corresponding MIDI event value and the synthesizer channel preset are updated.
        /// </remarks>
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
        /// Toggle sound on/off while keeping the MIDI sequencer running.
        /// </summary>
        /// <remarks>
        /// This method stores/restores the current global volume but does not flip the internal <see cref="mute"/> flag.
        /// The caller should manage the <see cref="mute"/> state if required.
        /// </remarks>
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

        /// <summary>Pause MIDI playback.</summary>
        public void Pause()
        {
            midiPlayer.MPTK_Pause();
        }
        /// <summary>Resume MIDI playback.</summary>
        public void UnPause()
        {
            midiPlayer.MPTK_UnPause();
        }

        /// <summary>Set absolute transpose (semitones).</summary>
        public void TransposeSet(int transpose)
        {
            midiPlayer.MPTK_Transpose = transpose;
        }

        /// <summary>Add a transpose offset to the current transpose value.</summary>
        public void TransposeAdd(int transpose)
        {
            midiPlayer.MPTK_Transpose += transpose;
        }

        /// <summary>Clear transpose (reset to zero).</summary>
        public void TransposeClear()
        {
            midiPlayer.MPTK_Transpose = 0;
        }

        /// <summary>
        /// Apply a pitch wheel change progressively on all channels for a specified duration.
        /// The change is interpolated over time.
        /// </summary>
        /// <param name="pitchTarget">Target pitch wheel normalized value (0..1). 0 = full down, 0.5 = center, 1 = full up.</param>
        /// <param name="durationMilli">Duration in milliseconds over which to apply the change.</param>
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
        /// Progressively change the pitch wheel for all channels then restore center (8192).
        /// Pitch wheel values:
        ///   0   = lowest bend (default +/- 2 semitones),
        ///   0.5 = centered (no bend),
        ///   1   = highest bend.
        /// </summary>
        private IEnumerator PitchChannelRoutine(float pitchTarget, float durationMilli)
        {
            float pitch = 0.5f; // centered value
            float waitMillisecond = 100f; // wait between each pitch adjustment
            float deltaPitchMilli = (pitchTarget - 0.5f) / durationMilli; // change per millisecond
            DateTime stop = DateTime.Now.AddMilliseconds(durationMilli);
            DateTime now = DateTime.Now;

            while (now < stop)
            {
                // Safety checks to exit if we reached the target.
                if (deltaPitchMilli < 0 && pitch <= pitchTarget) break;
                if (deltaPitchMilli > 0 && pitch >= pitchTarget) break;

                pitch += ((float)(DateTime.Now - now).TotalMilliseconds) * deltaPitchMilli;
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

            // Restore original pitch wheel center value for all channels.
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

        /// <summary>
        /// Apply a pitch multiplier to the underlying AudioSources used by active voices.
        /// This affects the played audio pitch (playback rate) rather than MIDI pitch wheel.
        /// </summary>
        /// <param name="pitchFactor">Multiplier applied to AudioSource.pitch (typical range 0.2 .. 2).</param>
        /// <param name="durationMilli">Duration in milliseconds (total effect duration).</param>
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

        /// <summary>
        /// Gradually adjust the AudioSource.pitch on active voices over time.
        /// Note: when the web player is enabled, each note may use its own AudioSource.
        /// </summary>
        private IEnumerator PitchAudioSourceRoutine(float pitchFactor, float durationMilli)
        {
            float duration = (durationMilli / 1000f) / 10f;
            // Apply the pitch factor 10 times spaced over the requested duration to create a gradual effect.
            for (int i = 0; i < 10; i++)
            {
                for (int v = 0; v < midiPlayer.ActiveVoices.Count; v++)
                {
                    fluid_voice voice = midiPlayer.ActiveVoices[v];
                    yield return null;
                    // Multiply the existing pitch by the requested factor.
                    voice.VoiceAudio.Audiosource.pitch *= pitchFactor;
                }
                yield return Routine.WaitForSeconds(durationMilli);
            }
        }

        void Update()
        {
            float speedClamp = 1f;

            // Calculate music playback speed from the player's speed when the level is active.
            if (gameManager.levelRunning && !gameManager.levelPaused)
            {
                // Min and max music speed are defined by the current level.
                TerrainLevel current = gameManager.terrainGenerator.CurrentLevel;
                float speedMusic = player.Speed * current.RatioSpeedMusic;
                speedClamp = Mathf.Clamp(speedMusic, current.MinSpeedMusic, current.MaxSpeedMusic);
            }

            // Avoid updating the MIDI speed every frame: only apply an update when the change exceeds a small threshold.
            if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 0.1f)
            {
                midiPlayer.MPTK_Speed = speedClamp;
                previousSpeed = speedClamp;
            }
        }
    }
}