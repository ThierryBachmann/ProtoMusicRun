using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    public PlayerController player;
    public ScoreManager scoreManager;
    public AudioSource collisionSound;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            player.speedMultiplier = 0.5f;
            scoreManager.coefficient = 1f;
            if (collisionSound) collisionSound.Play();
        }
    }
}
