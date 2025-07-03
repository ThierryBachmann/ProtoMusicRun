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

    private bool onCooldown;
    [SerializeField] private float cooldownDelay = 1.5f;
    [SerializeField] private float knockbackPower = 6f;   // distance ressentie
    void Start()
    {
    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (onCooldown) return;
        if (!hit.collider.CompareTag("Obstacle")) return;


        if (hit.collider.CompareTag("Obstacle"))
        {
            Debug.Log($"Le joueur a heurté un obstacle : {hit.collider.name} {hit.collider.tag}");
            MainMusic.MPTK_Pause(1000);
            Player.speedMultiplier = 0.5f;
            ScoreManager.coefficient = 1f;
            SoundManager.PlayCollisionSound();

            // 2. knock‑back
            Vector3 pushDir = Vector3.ProjectOnPlane(hit.normal, Vector3.up); // horizontale
            Player.ApplyKnockback(pushDir, knockbackPower);

            // 3. anti‑spam
            onCooldown = true;
            Invoke(nameof(ResetCooldown), cooldownDelay);
        }
    }
    void ResetCooldown() => onCooldown = false;
}
