using UnityEngine;
using MidiPlayerTK;

namespace MusicRun
{
    public class MidiTempoSync : MonoBehaviour
    {
        // If true, speed will change with tempo changes. Disabled by default (finally, not useful)
        private bool SpeedAsTempoChange = false;
        private float RatioTempoMusic = 0.4f;
        private float MinTempoMusic = 50f;
        private float MaxTempoMusic = 300f;

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

        public void Reset()
        {
            previousSpeed = -1;
        }
        public void Default()
        {
            previousSpeed = -1;
        }
        void Update()
        {
            if (SpeedAsTempoChange)
            {
                float speed = 50 + player.speedMultiplier * RatioTempoMusic;
                float speedClamp = Mathf.Clamp(speed, MinTempoMusic, MaxTempoMusic);
                if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 2f)
                {
                    Debug.Log($"player.speedMultiplier: {player.speedMultiplier} music tempo {speedClamp}");
                    midiPlayer.MPTK_Tempo = speedClamp;
                    previousSpeed = speedClamp;
                }
            }
            else
            {
                float speedClamp = 1f;
                if (gameManager.levelRunning)
                {
                    Level current = gameManager.TerrainGenerator.CurrentLevel;
                    float speed = player.speedMultiplier * current.RatioSpeedMusic;
                    speedClamp = Mathf.Clamp(speed, current.MinSpeedMusic, current.MaxSpeedMusic);
                }
                if (previousSpeed < 0f || Mathf.Abs(previousSpeed - speedClamp) > 0.1f)
                {
                    Debug.Log($"player.speedMultiplier: {player.speedMultiplier} music speed {speedClamp}");
                    midiPlayer.MPTK_Speed = speedClamp;
                    previousSpeed = speedClamp;
                }
            }
        }
    }
}