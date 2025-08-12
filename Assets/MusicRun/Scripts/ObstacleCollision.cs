using MidiPlayerTK;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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
        private MidiFilePlayer midiPlayer;
        private SoundManager SoundManager;

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;
            midiPlayer= gameManager.midiPlayer;
            SoundManager= gameManager.soundManager;

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
                Debug.Log($"Le joueur a heurté un obstacle : {hit.collider.name} {hit.collider.tag} applyShake:{applyShake}");
                cameraShake.TriggerShake(0.4f, 0.15f * applyShake, 2f); // durée, amplitude, amortissement
                midiPlayer.MPTK_Pause(1000);
                player.speedMultiplier = 0.5f;
                SoundManager.PlayCollisionSound();

                // 2. knock‑back
                Vector3 pushDir = Vector3.ProjectOnPlane(hit.normal, Vector3.up); // horizontale
                player.ApplyKnockback(pushDir, knockbackPower * applyShake);

                // 3. anti‑spam
                onCooldown = true;
                Invoke(nameof(ResetCooldown), cooldownDelay);
            }
        }
        void ResetCooldown() => onCooldown = false;
    }
}