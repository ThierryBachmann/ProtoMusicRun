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

    public float score;

    void Update()
    {
     
        if (player.goalHandler.forward > 0)
            score += player.goalHandler.forward * player.speedMultiplier * coefficient;
        else
            score += Time.deltaTime * player.goalHandler.forward;

    }

    public int GetDisplayedScore() => Mathf.FloorToInt(score);
}