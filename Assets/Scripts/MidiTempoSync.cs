using UnityEngine;
using MidiPlayerTK;

public class MidiTempoSync : MonoBehaviour
{
    public MidiFilePlayer midiPlayer;
    public PlayerController player;
    
    [Header("Ration Speed player vs Speed Music")]
    public float RatioSpeedMusic = 0.4f;
    public float MaxSpeedMusic = 3f;
    
    private float previousSpeed = -1;

    void Update()
    {
        //   GetSpeed=initialSpeed * speedMultiplier
        //   initialSpeed=5 (constant)
        //   speedMultiplier 0.5 --> 10
        //float speed = player.GetSpeed() / (player.initialSpeed * 1f);
        float speed = player.speedMultiplier * 0.5f;
        float speedClamp = Mathf.Clamp(speed, 0.1f, MaxSpeedMusic);
        if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 0.2f)
        {
            Debug.Log($"player.speedMultiplier: {player.speedMultiplier} music speed {speedClamp}");
            midiPlayer.MPTK_Speed = speedClamp;
            previousSpeed=speedClamp;
        }
    }
}