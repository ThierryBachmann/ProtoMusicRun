/*
 * Prototype Unity 3D : Scène "forêt musicale"
 * Objectifs :
 * - Génération dynamique de terrain
 * - Joueur en vue subjective
 * - Synchronisation musique MIDI avec Maestro MPTK
 * - Obstacles + système de score
 */

// === PlayerController.cs ===
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public bool enableMovement = true;
    public GameManager gameManager;
    public GoalHandler goalHandler;
    public FirebaseLeaderboard leaderboard;
    public ScoreManager scoreManager;
    public LeaderboardDisplay leaderboardDisplay;

    [Header("Mouvement avant")]
    public float speedMultiplier;
    public float initialSpeed;

    [Header("Orientation")]
    public float turnSpeed = 90f; // vitesse de rotation fluide en °/s
    public float maxAngle = 45f;

    [Header("Saut & gravité")]
    private float gravity = 9.81f;
    public float jumpForce;

    [Header("Knock‑back")]
    public float knockbackDecay = 4f;  // plus grand = ralentit plus vite


    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 verticalVelocity;  // stocke la composante Y du saut/gravité
    private float targetAngle = 0f; // angle actuel utilisé pour la rotation
    private float currentAngle = 0f;
    private Vector3 knockback = Vector3.zero;
    public int playerPosition = 99999;
    public long playerBestScore = 0;
    public long playerLastScore = 0;

    void Awake()
    {
        goalHandler.OnLevelCompleted += OnLevelCompleted;
        leaderboard.OnLeaderboardLoaded += OnLeaderboardLoaded;
        leaderboard.OnScoreSubmitted += OnScoreSubmitted;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        leaderboardDisplay.Hide();
    }

    private void OnLeaderboardLoaded(List<PlayerScore> scores)
    {
        Debug.Log($"=== PLAYER RANK {leaderboard.GetPlayerName()} === ");
        PlayerScore playerRank = scores.Find(s => s.playerName == leaderboard.GetPlayerName());
        if (playerRank != null)
            Debug.Log($"Player Rank {playerRank.playerName:20}: {playerRank.score} pts " +
                 $"(Time: {playerRank.completionTime:F1}s, Efficiency: {playerRank.pathEfficiency:F2})");
        else
            Debug.Log($"Player Rank not found");

    }
    private void OnLevelCompleted(bool success)
    {
        gameManager.LevelCompleted();
        speedMultiplier = Mathf.MoveTowards(speedMultiplier, 0f, Time.deltaTime * 15f);
        HandleMovement(false);
        playerLastScore = scoreManager.score;
        PlayerScore playerScore = new PlayerScore(
                     leaderboard.GetPlayerName(),
                     scoreManager.score,
                     999,
                     999,
                     999,
                     1
                      );
        leaderboard.SubmitScore(playerScore);
        //leaderboardDisplay.Show(this);
    }

    private void OnScoreSubmitted(bool success)
    {
        leaderboard.GetPlayerRank((s) =>
        {
            if (s != null)
            {
                playerPosition = s.playerPosition;
                playerBestScore = s.score;
                leaderboardDisplay.RefreshPlayerScore(this);
            }
        });
    }

    public void ApplyKnockback(Vector3 direction, float strength)
    {
        knockback = direction.normalized * strength;
    }

    public void ResetPosition(Transform startPosition)
    {
        CharacterController cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = startPosition.position;
        transform.rotation = startPosition.rotation;
        currentAngle = 0;
        targetAngle = 0;
        cc.enabled = true;
    }

    void Update()
    {
        if (enableMovement && gameManager.levelRunning && !goalHandler.goalReached)
        {
            HandleInput();
            HandleRotation();
            HandleMovement();

            // Remontée progressive du multiplicateur de vitesse
            // Il rapproche doucement speedMultiplier de 10f a une vitesse de 0.5 par seconde.
            // Selon la douceur voulue(par ex. 1f = plus rapide, 0.1f = plus lent)
            speedMultiplier = Mathf.MoveTowards(speedMultiplier, 10f, Time.deltaTime * 0.1f);
        }
        else
        {
            HandleInput();
            HandleRotation();
        }
    }

    void HandleInput()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            targetAngle -= turnSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            targetAngle += turnSpeed * Time.deltaTime;
        }

        // Clamp l'angle cible
        //     targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);
    }


    void HandleRotation()
    {
        // Interpolation douce entre l'angle actuel et l'angle cible
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 5f); // facteur de lissage ajustable

        // Appliquer au transform
        transform.rotation = Quaternion.Euler(0, currentAngle, 0);
    }
    void HandleMovement(bool stopDown = false)
    {
        // Avance “normale”
        Vector3 forwardMove = transform.forward * initialSpeed * speedMultiplier;

        // Applique le knock‑back et le fait décélérer progressivement
        if (knockback.sqrMagnitude > 0.01f)
        {
            forwardMove += knockback;
            knockback = Vector3.Lerp(knockback, Vector3.zero, Time.deltaTime * knockbackDecay);
        }
        else
        {
            knockback = Vector3.zero; // sécurité
        }

        // Gestion saut / gravité
        if (controller.isGrounded)
        {
            verticalVelocity.y = -1f;
            if (Input.GetKeyDown(KeyCode.Space)) verticalVelocity.y = jumpForce;
        }
        else
        {
            verticalVelocity.y -= gravity * Time.deltaTime;
        }

        Vector3 finalMove = forwardMove;
        finalMove.y = verticalVelocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    public float GetSpeed() => initialSpeed * speedMultiplier;
}