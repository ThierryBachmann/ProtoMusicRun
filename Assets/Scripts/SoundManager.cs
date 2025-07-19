using MidiPlayerTK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public MidiStreamPlayer MidiSound;
    public List<Jingle> Jingles;
    private Dictionary<string, List<SoundEvent>> jingleDict;

    void Start()
    {
        jingleDict = new Dictionary<string, List<SoundEvent>>();
        foreach (var jingle in Jingles)
        {
            foreach (SoundEvent soundEvent in jingle.soundEvents)
                soundEvent.BuildMPTKEvent();
            jingleDict.Add(jingle.name, jingle.soundEvents);
        }
    }
    public void PlayCollisionSound()
    {
        if (MidiSound)
        {
            List<SoundEvent> ev;
            if (jingleDict.TryGetValue("Collision", out ev))
                StartCoroutine(PlaySoundCoroutine(ev));
            else
                Debug.LogError("Collision not found");
        }
    }
    public IEnumerator PlaySoundCoroutine(List<SoundEvent> sounds)
    {
        foreach (SoundEvent sound in sounds)
        {
            switch (sound.action)
            {
                case SoundEvent.Action.WAIT:
                    yield return new WaitForSeconds(sound.duration / 1000f);
                    break;
                case SoundEvent.Action.NOTEON:
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
    public int value;
    public int channel;
    public int duration;

    public void BuildMPTKEvent()
    {
        switch (action)
        {
            case Action.NOTEON:
                mptkEvent = new MPTKEvent() { Command = MPTKCommand.NoteOn, Channel = channel, Value = value, Duration = duration };
                break;

            case Action.PRESET:
                mptkEvent = new MPTKEvent() { Command = MPTKCommand.PatchChange, Channel = channel, Value = value };
                break;
        }
    }
}

