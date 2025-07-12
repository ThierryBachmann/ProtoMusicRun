using UnityEngine;

/// <summary>
/// on calcule à chaque frame la composante du déplacement qui rapproche vraiment le joueur du but.
/// Principe en une ligne
///     Score += projection du déplacement sur la direction “Start → Goal” × multiplicateur
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public PlayerController player;
    public float coefficient = 1f;

    public long score;

    void Update()
    {
        if (!player.goalHandler.goalReached)
        {
            float bonusDirection = player.goalHandler.goalAngle >= -15f && player.goalHandler.goalAngle <= 15f ? 1f : -1f;
            score += (long)(bonusDirection * player.speedMultiplier * coefficient);
            if (score < 0) score = 0;
        }
    }

    public int GetDisplayedScore() => Mathf.FloorToInt(score);
}