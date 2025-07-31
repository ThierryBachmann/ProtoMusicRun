using UnityEngine;
using MidiPlayerTK;

namespace MusicRun
{
    public class MidiTempoSync : MonoBehaviour
    {
        [Header("Ration Speed player vs Speed Music")]
        public float RatioSpeedMusic = 0.4f;
        public float MaxSpeedMusic = 3f;

        private float previousSpeed = -1;

        private GameManager gameManager;
        private PlayerController player;
        private MidiFilePlayer midiPlayer;

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.Player;
            midiPlayer = gameManager.MidiPlayer;
        }

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
                previousSpeed = speedClamp;
            }
        }
    }
}