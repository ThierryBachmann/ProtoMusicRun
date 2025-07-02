using MidiPlayerTK;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class SoundManager : MonoBehaviour
{
    public MidiStreamPlayer MidiSound;

    public MPTKEvent SoundCollision1;

    void Start()
    {
        SoundCollision1 = new MPTKEvent() { Command = MPTKCommand.NoteOn, Channel = 9, Duration = 1000, Value = 60 };
    }
    public void PlayCollisionSound()
    {
        if (MidiSound)
        {
            MidiSound.MPTK_PlayDirectEvent(SoundCollision1);
        }
    }
}
