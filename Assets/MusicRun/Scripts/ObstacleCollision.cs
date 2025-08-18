using MidiPlayerTK;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicRun
{
    public class ObstacleCollision : MonoBehaviour
    {
        public CameraShake cameraShake;

        private bool onCooldown;
        [SerializeField] private float cooldownDelay = 1.5f;
        [SerializeField] private float knockbackPower = 6f;

        private GameManager gameManager;
        private PlayerController player;
        private MidiManager midiManager;
        private SoundManager soundManager;
        private ScoreManager scoreManager;

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
            midiManager = gameManager.midiManager;
            soundManager = gameManager.soundManager;
            scoreManager = gameManager.scoreManager;

            if (cameraShake == null && Camera.main != null)
            {
                Debug.LogWarning("cameraShake is null");
                cameraShake = Camera.main.GetComponent<CameraShake>();
            }
        }

        void Start()
        {
        }
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (onCooldown) return;

            if (hit.collider.CompareTag("Obstacle"))
            {
                float applyShake = Mathf.Clamp(player.GetSpeed() / 7f, 0.5f, 3f);
                Debug.Log($"obstacle hit by player : {hit.collider.name} {hit.collider.tag} applyShake:{applyShake}");
                scoreManager.EndBonus();
                cameraShake.TriggerShake(0.4f, 0.15f * applyShake, 2f); 
                midiManager.midiPlayer.MPTK_Pause(1000);
                player.speedMultiplier = 0.5f;
                soundManager.PlayCollisionSound();

                Vector3 pushDir = Vector3.ProjectOnPlane(hit.normal, Vector3.up); 
                player.ApplyKnockback(pushDir, knockbackPower * applyShake);

                onCooldown = true;
                Invoke(nameof(ResetCooldown), cooldownDelay);
            }
        }
        void ResetCooldown() => onCooldown = false;
    }
}