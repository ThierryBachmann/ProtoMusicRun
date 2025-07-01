using MidiPlayerTK;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ObstacleCollision : MonoBehaviour
{
    public PlayerController Player;
    public ScoreManager ScoreManager;
    public MidiFilePlayer MainMusic;
    public SoundManager SoundManager;

    void Start()
    {
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Obstacle"))
        {
            Debug.Log("Le joueur a heurté un obstacle : " + hit.collider.name);
            MainMusic.MPTK_Pause(1000);
            Player.speedMultiplier = 0.5f;
            ScoreManager.coefficient = 1f;
            SoundManager.PlayCollisionSound();
        }
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter: " + other.gameObject.name);
    }
        void OnCollisionEnter(Collision collision)
    {
        Debug.Log("ObstacleCollision: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            MainMusic.MPTK_Pause(1000);
            Player.speedMultiplier = 0.5f;
            ScoreManager.coefficient = 1f;
            SoundManager.PlayCollisionSound();
        }
    }
}
