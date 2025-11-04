/*
 * Prototype Unity 3D : Scène "forêt musicale"
 * Objectifs :
 * - Génération dynamique de terrain
 * - Joueur en vue subjective
 * - Synchronisation musique MIDI avec Maestro MPTK
 * - Obstacles + système de score
 */

// === PlayerController.cs ===
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MusicRun
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Debug")]
        public bool enableMovement = true;
        public bool isJumping;
        public Vector3 verticalVelocity;
        public float targetAngle = 0f;
        public float currentAngle = 0f;
        public Vector3 knockback = Vector3.zero;

        [Header("Movement")]
        public float MinSpeed;
        public float MaxSpeed;
        public float Acceleration = 0.1f;
        public float Speed;

        [Header("Orientation")]
        public float TurnSpeed = 0.25f;
        public float MaxAngle = 45f;

        [Header("Jump")]
        public float gravity = 12.81f;
        public float JumpForce = 0.15f;

        [Header("Knock‑back")]
        public float knockbackDecay = 4f;

        [Header("Score")]
        public string playerName = "";
        public int playerPosition = 99999;
        public long playerBestScore = 0;
        public long playerLastScore = 0;

        private CharacterController controller;
        private GameManager gameManager;
        private TerrainGenerator terrainGenerator;
        private ScoreManager scoreManager;
        private TouchEnabler touchEnabler;

        public DateTime timeStartLevel;

        [Header("For readonly")]
        private Vector2Int currentPlayerChunk;

        public Vector2Int CurrentPlayerChunk { get => currentPlayerChunk; }

        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
            terrainGenerator = gameManager.terrainGenerator;
            controller = GetComponent<CharacterController>();
            scoreManager = gameManager.scoreManager;
            touchEnabler = gameManager.touchEnabler;
            gameManager.settingScreen.OnSettingChange += OnSettingChange;
            playerName = PlayerPrefs.GetString("player_name");
        }

        void Start()
        {
            gameManager.settingScreen.SetValue();
        }

        void OnTriggerEnter(Collider collider)
        {
            Debug.Log($"PlayerController trigger {collider.tag}");
            if (collider.CompareTag("Bonus") || collider.CompareTag("Malus"))
            {
                gameManager.bonusManager.TriggerBonus(collider);
            }
            else if (collider.CompareTag("Instrument"))
            {
                gameManager.bonusManager.TriggerInstrument(collider);
            }
        }

        private void OnSettingChange()
        {
            Debug.Log("PlayerController OnSettingChange");
            gameManager.headerDisplay.SetTitle();
            PlayerPrefs.SetString("player_name", playerName);
            PlayerPrefs.Save();
        }

        public void LevelStarted()
        {
            ResetPosition();
            timeStartLevel = DateTime.Now;
            Speed = MinSpeed;
        }

        public void LevelEnded()
        { 
        //    Speed = MinSpeed;
        //    gameManager.bonusManager.EndBonus();
            HandleMovement(Vector3.zero);
        }

        public void ApplyKnockback(Vector3 direction, float strength)
        {
            knockback = direction.normalized * strength;
        }

        public void ResetPosition()
        {
            Speed = MinSpeed;

            controller.enabled = false;
            Vector3 previous = transform.position;
            Transform start = gameManager.terrainGenerator.StartGO.transform;
            transform.position = start.position;
            transform.rotation = start.rotation;
            currentAngle = 0;
            targetAngle = 0;
            TerrainGenerator.PositionOnHighestTerrain(transform);
            Debug.Log($"Player ResetPosition from {previous} to {transform.position}");
            controller.enabled = true;
        }

        public IEnumerator TeleportPlayer(Vector3 targetPosition)
        {
            controller.enabled = false;
            yield return null; // Wait one frame
            transform.position = targetPosition;
            yield return null; // Wait one frame
            controller.enabled = true;
        }

        void Update()
        {
            Vector3 forwardMove = Vector3.zero;
            if (enableMovement && gameManager.levelRunning && !gameManager.goalHandler.goalReached && !gameManager.levelPaused)
            {
                // Slowly increase speed multiplier until MaxSpeed at Acceleration per second
                Speed = Mathf.MoveTowards(Speed, MaxSpeed, Time.deltaTime * Acceleration);
                forwardMove = transform.forward * Speed;
            }
            HandleInput();
            HandleRotation();
            HandleMovement(forwardMove);

            if (gameManager.gameRunning)
            {
                Vector2Int playerChunk = terrainGenerator.PositionToChunk(transform.position);
                if (playerChunk != currentPlayerChunk)
                {
                    //Debug.Log($"Player enters in a chunk: x={transform.position.x} z={transform.position.z} --> playerChunk: {playerChunk}");
                    currentPlayerChunk = playerChunk;
                    terrainGenerator.UpdateChunks(currentPlayerChunk);
                }
            }
        }

        void HandleInput()
        {
            if (gameManager.actionLevel.leftButton.IsHeld || touchEnabler.TurnLeftIsPressed)
                targetAngle -= TurnSpeed * 250f * Time.deltaTime;
            else if (gameManager.actionLevel.rightButton.IsHeld || touchEnabler.TurnRightIsPressed)
                targetAngle += TurnSpeed * 250f * Time.deltaTime;

            if (touchEnabler.SwipeHorizontalValue != 0f)
                targetAngle += TurnSpeed * touchEnabler.SwipeHorizontalValue * Time.deltaTime;

            if (!isJumping)
            {
                if (gameManager.actionLevel.jumpButton.IsHeld || touchEnabler.TurnUpIsPressed)
                {
                    verticalVelocity.y = JumpForce * 50f;
                    isJumping = true;
                }
                if (touchEnabler.SwipeVerticalValue != 0f)
                {
                    verticalVelocity.y = JumpForce * touchEnabler.SwipeVerticalValue;
                    isJumping = true;
                    touchEnabler.ResetSwipeVertical();
                }
            }


            if (transform.position.y < 0f)
                StartCoroutine(TeleportPlayer(new Vector3(transform.position.x, 4, transform.position.z)));

            // Pour tester 
            if (Input.GetKeyDown(KeyCode.Delete))
                StartCoroutine(TeleportPlayer(new Vector3(transform.position.x, -1, transform.position.z)));
        }

        void HandleRotation()
        {
            //Smooth interpolation between the current angle and the target angle, with an adjustable smoothing factor.
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Euler(0, currentAngle, 0);
        }

        void HandleMovement(Vector3 forwardMove)
        {
            if (!controller.enabled)
                return;

            // knock‑back, slowdown
            if (knockback.sqrMagnitude > 0.01f)
            {
                forwardMove += knockback;
                knockback = Vector3.Lerp(knockback, Vector3.zero, t: Time.deltaTime * knockbackDecay);
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
    }
}