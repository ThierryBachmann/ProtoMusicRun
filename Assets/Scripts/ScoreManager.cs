using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public PlayerController player;
    public float score;
    public float coefficient = 1f;

    void Update()
    {
        score += Time.deltaTime * player.GetSpeed() * coefficient;
    }
}