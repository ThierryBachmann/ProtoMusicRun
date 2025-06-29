using UnityEngine;
using MidiPlayerTK; // Assure-toi d'avoir installé Maestro MPTK

public class MidiTempoSync : MonoBehaviour
{
    public MidiFilePlayer midiPlayer;
    public PlayerController player;

    void Update()
    {
        float tempo = Mathf.Clamp(player.GetSpeed() * 30f, 60f, 240f);
        midiPlayer.MPTK_Tempo = tempo;
    }
}