using MidiPlayerTK;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class ObstacleCollision : MonoBehaviour
{
    public PlayerController Player;
    public ScoreManager ScoreManager;
    public MidiFilePlayer MainMusic;
    public SoundManager SoundManager;
    public CameraShake cameraShake;

    private bool onCooldown;
    [SerializeField] private float cooldownDelay = 1.5f;
    [SerializeField] private float knockbackPower = 6f;   

    void Awake()
    {
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
            float applyShake = Mathf.Clamp(Player.GetSpeed() / 7f, 0.5f, 3f);
            Debug.Log($"Le joueur a heurté un obstacle : {hit.collider.name} {hit.collider.tag} applyShake:{applyShake}");
            cameraShake.TriggerShake(0.4f, 0.15f * applyShake, 2f); // durée, amplitude, amortissement
            MainMusic.MPTK_Pause(1000);
            Player.speedMultiplier = 0.5f;
            SoundManager.PlayCollisionSound();

            // 2. knock‑back
            Vector3 pushDir = Vector3.ProjectOnPlane(hit.normal, Vector3.up); // horizontale
            Player.ApplyKnockback(pushDir, knockbackPower * applyShake);

            // 3. anti‑spam
            onCooldown = true;
            Invoke(nameof(ResetCooldown), cooldownDelay);
        }
    }
    void ResetCooldown() => onCooldown = false;
}
