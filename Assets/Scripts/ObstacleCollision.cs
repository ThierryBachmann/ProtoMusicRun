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
    private float resetDelay = 2f; // Dur�e pendant laquelle la collision est ignor�e

    void Start()
    {
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hasCollided || Player.IsBeingPushed()) return;

        if (hit.collider.CompareTag("Obstacle"))
        {
            Debug.Log($"Le joueur a heurt� un obstacle : {hit.collider.name} {hit.collider.tag}");
            MainMusic.MPTK_Pause(1000);
            Player.speedMultiplier = 0.5f;
            ScoreManager.coefficient = 1f;
            SoundManager.PlayCollisionSound();

            // Recul du player
            Vector3 pushBack = -hit.normal * 2f; // Pousse en arri�re selon la normale du contact
            PlayerController controller = Player.GetComponent<PlayerController>();
            controller.ForceMove(pushBack); // Cr�e cette m�thode pour appliquer ce d�placement

            hasCollided = true;
            Invoke(nameof(ResetCollision), resetDelay);
        }
    }

    private void ResetCollision()
    {
        hasCollided = false;
    }
}
