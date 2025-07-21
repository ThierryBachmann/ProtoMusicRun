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
    [Header("Debug")]
    public bool enableMovement = true;
    public bool isJumping;
    public Vector3 verticalVelocity;  // stocke la composante Y du saut/gravité
    public float targetAngle = 0f; // angle actuel utilisé pour la rotation
    public float currentAngle = 0f;
    public Vector3 knockback = Vector3.zero;

    [Header("Movement")]
    public float speedMultiplier;
    public float initialSpeed;

    [Header("Orientation")]
    public float turnSpeed = 90f; // vitesse de rotation fluide en °/s
    public float maxAngle = 45f;

    [Header("Jump")]
    private float gravity = 9.81f;
    public float jumpForce;

    [Header("Knock‑back")]
    public float knockbackDecay = 4f;  // plus grand = ralentit plus vite

    [Header("Score")]
    public int playerPosition = 99999;
    public long playerBestScore = 0;
    public long playerLastScore = 0;


    [Header("GameObject")]
    private CharacterController controller;
    public GameManager gameManager;
    public GoalHandler goalHandler;
    public FirebaseLeaderboard leaderboard;
    public ScoreManager scoreManager;
    public LeaderboardDisplay leaderboardDisplay;

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

    public void ResetPlayer(Transform startPosition)
    {
        ResetPosition(startPosition);
        speedMultiplier = 0.5f;
        goalHandler.goalReached = false;
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

        HandleMovement(Vector3.zero);
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
        Vector3 forwardMove = Vector3.zero;
        if (enableMovement && gameManager.levelRunning && !goalHandler.goalReached)
        {
            // Slowly increase speed multiplier until 10 at 0.1 per second
            speedMultiplier = Mathf.MoveTowards(speedMultiplier, 10f, Time.deltaTime * 0.1f);
            forwardMove = transform.forward * initialSpeed * speedMultiplier;
        }
        HandleInput();
        HandleRotation();
        HandleMovement(forwardMove);
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
        if (!isJumping && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity.y = jumpForce;
            isJumping = true;
        }
        // Clamp l'angle cible
        //     targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);
    }

    void HandleRotation()
    {
        //Smooth interpolation between the current angle and the target angle, with an adjustable smoothing factor.
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 5f); 
        transform.rotation = Quaternion.Euler(0, currentAngle, 0);
    }

    void HandleMovement(Vector3 forwardMove)
    {
        // Avance “normale”
        //Vector3 forwardMove = transform.forward * initialSpeed * speedMultiplier;

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

        // Update vertical velocity
        if (!isJumping)
        {
            verticalVelocity.y = -1f; // Very small downward move to set the player on the ground
        }
        else
        {
            verticalVelocity.y -= gravity * Time.deltaTime;
        }

        // Combine movement
        Vector3 finalMove = forwardMove;
        finalMove.y = verticalVelocity.y;
        
        //bool grounded = controller.isGrounded;
        // Before you call controller.Move(...), isGrounded contains the value from the previous frame.
        // After Move(...), Unity recalculates collisions, so the new value of isGrounded depends on the result of the Move.
        controller.Move(finalMove * Time.deltaTime);
        
        // Debug.Log($"isGrounded avant:{grounded} apres:{controller.isGrounded} finalMove:{finalMove} isJumping:{isJumping}");
        
        if (controller.isGrounded)
            isJumping = false;
    }

    public float GetSpeed() => initialSpeed * speedMultiplier;
}