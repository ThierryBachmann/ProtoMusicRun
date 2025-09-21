using MidiPlayerTK;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MusicRun
{
    public class ObstacleCollision : MonoBehaviour
    {

        private bool onCooldown;
        [SerializeField] private float cooldownDelay = 1.5f;
        [SerializeField] private float knockbackPower = 6f;

        private GameManager gameManager;
        private PlayerController player;

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            player = gameManager.playerController;

        }

        void Start()
        {
        }
        void OnControllerColliderHit(ControllerColliderHit hit)
        {

            if (onCooldown) return;

            if (hit.collider.CompareTag("Obstacle"))
            {
                float applyShake = Mathf.Clamp(player.Speed / 7f, 0.5f, 3f);
                //Debug.Log($"obstacle hit by player : {hit.collider.name} {hit.collider.tag} applyShake:{applyShake}");
                gameManager.bonusManager.EndBonus(); 
                if (gameManager.cameraSelected != null)
                {
                    CameraShake cameraShake = gameManager.cameraSelected.GetComponent<CameraShake>();
                    if (cameraShake != null)
                        cameraShake.TriggerShake(0.4f, 0.15f * applyShake, 2f);
                    else
                        Debug.LogWarning("cameraShake is null");
                }
                else
                    Debug.LogWarning("No current camera");

                gameManager.playerController.Speed = gameManager.playerController.MinSpeed;
                gameManager.midiManager.ApplyPitchChannel(0.2f, 500f);

                Vector3 pushDir = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                player.ApplyKnockback(pushDir, knockbackPower * applyShake);

                onCooldown = true;
                Invoke(nameof(ResetCooldown), cooldownDelay);
            }
        }
        void ResetCooldown() => onCooldown = false;
    }
}