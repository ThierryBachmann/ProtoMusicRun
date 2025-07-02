using MidiPlayerTK;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ObstacleCollision : MonoBehaviour
{
    public PlayerController Player;
    public ScoreManager ScoreManager;
    public MidiFilePlayer MainMusic;
    public SoundManager SoundManager;

    private bool hasCollided = false;
    private float resetDelay = 2f; // Durée pendant laquelle la collision est ignorée

    void Start()
    {
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hasCollided || Player.IsBeingPushed()) return;

        if (hit.collider.CompareTag("Obstacle"))
        {
            Debug.Log($"Le joueur a heurté un obstacle : {hit.collider.name} {hit.collider.tag}");
            MainMusic.MPTK_Pause(1000);
            Player.speedMultiplier = 0.5f;
            ScoreManager.coefficient = 1f;
            SoundManager.PlayCollisionSound();

            // Recul du player
            Vector3 pushBack = -hit.normal * 2f; // Pousse en arrière selon la normale du contact
            PlayerController controller = Player.GetComponent<PlayerController>();
            controller.ForceMove(pushBack); // Crée cette méthode pour appliquer ce déplacement

            hasCollided = true;
            Invoke(nameof(ResetCollision), resetDelay);
        }
    }

    private void ResetCollision()
    {
        hasCollided = false;
    }
}
