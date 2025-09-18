using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MusicRun
{
    /// <summary>
    /// Create music jingles. Works like macro over MIDI to build short MIDI sequence.
    /// MIDI sequences are defined in the inspector and play by the game on scenario events.
    /// Not used here.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {

        public MidiStreamPlayer MidiSound;
        public bool RebuildJingles;
        public List<Jingle> Jingles;
        private Dictionary<string, List<SoundEvent>> jingleDict;

        void Start()
        {
            BuildJingles();
        }

        private void BuildJingles()
        {
            jingleDict = new Dictionary<string, List<SoundEvent>>();
            foreach (var jingle in Jingles)
            {
                foreach (SoundEvent soundEvent in jingle.soundEvents)
                    soundEvent.BuildMPTKEvent();
                Debug.Log($"Jingle {jingle.name} rebuild");
                jingleDict.Add(jingle.name, jingle.soundEvents);
            }
        }

        public void PlayCollisionSound()
        {
            StartCoroutine(PlaySoundCoroutine("Collision"));
        }

        /* no need to dynamically rebuild jingles. They are all defined at start
        private float timeInterval = 0.5f; // Time interval in seconds
        private float timer = 0.0f;
        public void Update()
        {
            // Increment the timer by the time since the last frame
            timer += Time.deltaTime;

            // Check if the timer has reached the specified interval
            if (timer >= timeInterval)
            {
                if (RebuildJingles)
                {
                    RebuildJingles = false;
                    BuildJingles();
                }
                timer = 0.0f;
            }
        }
        */

        public IEnumerator PlaySoundCoroutine(string name)
        {
            List<SoundEvent> sounds;
            if (!jingleDict.TryGetValue(name, out sounds))
                Debug.LogWarning($"Sound {name} not found");
            else
                foreach (SoundEvent sound in sounds)
                {
                    switch (sound.action)
                    {
                        case SoundEvent.Action.WAIT:
                            yield return new WaitForSeconds(sound.duration / 1000f);
                            break;
                        case SoundEvent.Action.NOTEON:
                        case SoundEvent.Action.PRESET:
                            MidiSound.MPTK_PlayDirectEvent(sound.mptkEvent);
                            break;
                    }
                }
        }
    }


    [Serializable]
    public class Jingle
    {
        public string name;
        public List<SoundEvent> soundEvents;
    }


    [Serializable]
    public class SoundEvent
    {
        public enum Action
        {
            WAIT,
            NOTEON,
            PRESET,
        }
        public MPTKEvent mptkEvent;
        public Action action;
        [Range(0, 127)]
        public int value;
        [Range(0, 127)]
        public int channel;
        [Range(0, 10000)]
        public int duration;
        [Range(0, 127)]
        public int velocity;

        public void BuildMPTKEvent()
        {
            switch (action)
            {
                case Action.NOTEON:
                    mptkEvent = new MPTKEvent()
                    {
                        Command = MPTKCommand.NoteOn,
                        Channel = channel,
                        Value = value,
                        Duration = duration,
                        Velocity = velocity
                    };
                    break;

                case Action.PRESET:
                    mptkEvent = new MPTKEvent() { Command = MPTKCommand.PatchChange, Channel = channel, Value = value };
                    break;
            }
        }
    }
}
