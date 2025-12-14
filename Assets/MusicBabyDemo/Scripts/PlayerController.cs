
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
        [Header("Current player chunk - for readonly.")]
        public Vector2Int currentPlayerChunk;
        [Header("Debug Player ")]
        public bool enableMovement = true;
        public bool isJumping;
        public Vector3 verticalVelocity;
        public float targetAngle = 0f;

        /// <summary>
        /// Current player direction in degrees
        /// </summary>
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
            Debug.Log($"-player- PlayerController trigger {collider.tag}");
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
            Debug.Log("-player- PlayerController OnSettingChange");
            gameManager.headerDisplay.SetTitle();
            PlayerPrefs.SetString("player_name", playerName);
            PlayerPrefs.Save();
        }

        public void LevelStarted()
        {
            //ResetPosition();
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

        public void ResetGameStop()
        {
            Speed = MinSpeed;
            transform.position = Vector3.zero + Vector3.up;
            transform.rotation = Quaternion.identity;
            StartCoroutine(TeleportPlayerRoutine(transform.position, Vector3.forward));
        }

        public void TeleportToStart()
        {
            Vector3 directionToGoal = (terrainGenerator.currentGoal.transform.position - terrainGenerator.currentStart.transform.position).normalized;
            Debug.Log($"-player- Teleport player from {transform.position} to {terrainGenerator.currentStart.transform.position} direction: {directionToGoal} ");
            transform.position = terrainGenerator.currentStart.transform.position;

            StartCoroutine(TeleportPlayerRoutine(new Vector3(transform.position.x, 1, transform.position.z), directionToGoal));
        }
        public void TeleportToGoal()
        {
            transform.position = terrainGenerator.currentGoal.transform.position - Vector3.back * 2f;
            Vector3 directionToGoal = (terrainGenerator.currentGoal.transform.position - transform.position).normalized;
            Debug.Log($"-player- Teleport player from {transform.position} to {terrainGenerator.currentStart.transform.position} direction: {directionToGoal} ");

            StartCoroutine(TeleportPlayerRoutine(new Vector3(transform.position.x, 1, transform.position.z), directionToGoal));
        }

        public IEnumerator TeleportPlayerRoutine(Vector3 targetPosition, Vector3 directionToFace)
        {
            controller.enabled = false;
            yield return null; // Wait one frame
            transform.position = targetPosition;
            transform.rotation = Quaternion.LookRotation(directionToFace);
            currentAngle = targetAngle = transform.eulerAngles.y;
            yield return null; // Wait one frame
            controller.enabled = true;
        }

        void Update()
        {
            if (terrainGenerator != null && terrainGenerator.currentGoal != null)
            {
                Vector3 directionToGoal = (terrainGenerator.currentGoal.transform.position - transform.position).normalized;
                Debug.DrawRay(transform.position, directionToGoal * 5f, Color.red, 2f);
            }

            Vector3 forwardMove = Vector3.zero;
            if (enableMovement && gameManager.levelRunning && !gameManager.goalHandler.goalReached && !gameManager.levelPaused)
            {
                // Slowly increase speed multiplier until MaxSpeed at Acceleration per second
                Speed = Mathf.MoveTowards(Speed, MaxSpeed, Time.deltaTime * Acceleration);
                forwardMove = transform.forward * Speed;
            }

            if (enableMovement)
                HandleInput();

            if (transform.position.y < 0f)
                StartCoroutine(TeleportPlayerRoutine(
                    new Vector3(transform.position.x, 0.5f, transform.position.z),
                    //transform.rotation.y));
                    new Vector3(Mathf.Cos(currentAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(currentAngle * Mathf.Deg2Rad))));

            if (enableMovement)
            {
                HandleRotation();
                HandleMovement(forwardMove);
            }

            if (gameManager.gameRunning)
            {
                Vector2Int playerChunk = terrainGenerator.PositionToChunk(transform.position);
                if (playerChunk != currentPlayerChunk)
                {
                    // Player change to another chunk.
                    Debug.Log($"-player- enters in a chunk: x={transform.position.x} z={transform.position.z} --> playerChunk: {playerChunk}");
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
        }

        void HandleRotation()
        {
            //Smooth interpolation between the current angle and the target angle (degree), with an adjustable smoothing factor.
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