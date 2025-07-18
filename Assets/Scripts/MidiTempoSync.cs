using UnityEngine;
using MidiPlayerTK;

public class MidiTempoSync : MonoBehaviour
{
    public MidiFilePlayer midiPlayer;
    public PlayerController player;
    private float previousSpeed = -1;

    void Update()
    {
        float speed = Mathf.Clamp(player.GetSpeed() / (player.initialSpeed * 1f), 0.1f, 3.5f);
        if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speed) > 0.1f)
        {
            Debug.Log($"music speed {speed}");
            midiPlayer.MPTK_Speed = speed;
            previousSpeed=speed;
        }
    }
}