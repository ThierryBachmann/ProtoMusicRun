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
        public float speedMultiplier;
        public float initialSpeed;

        [Header("Orientation")]
        public float turnSpeed = 90f;
        public float maxAngle = 45f;

        [Header("Jump")]
        public float gravity = 12.81f;
        public float jumpForce;

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
            touchEnabler= gameManager.touchEnabler;
            gameManager.settingScreen.OnSettingChange += OnSettingChange;
            playerName = PlayerPrefs.GetString("player_name");
        }

        void Start()
        {
            gameManager.settingScreen.SetValue();
        }

        //private void OnEnable()
        //{
        //    controls.Gameplay.Enable();
        //    controls.Gameplay.Swipe.performed += OnSwipe;
        //    controls.Gameplay.Swipe.started += Swipe_started;
        //    controls.Gameplay.TurnLeft.performed += (cb) => { Debug.Log($"TurnLeft.performed {controls.Gameplay.TurnLeft.IsPressed()}"); };
        //    controls.Gameplay.TurnLeft.canceled += (cb) => { Debug.Log("TurnLeft.canceled"); };
        //    controls.Gameplay.TurnRight.performed += (cb) => { Debug.Log("TurnRight.performed"); };
        //    controls.Gameplay.TurnRight.canceled += (cb) => { Debug.Log("TurnRight.canceled"); };
        //}

        //public Vector2 startPos;
        //private void Swipe_started(InputAction.CallbackContext obj)
        //{
        //    startPos = controls.Gameplay.Swipe.ReadValue<Vector2>();
        //    Debug.Log($"Swipe startPos {startPos}");
        //}

        //private void OnDisable()
        //{
        //    controls.Gameplay.Disable();
        //}

        //private void OnSwipe(InputAction.CallbackContext ctx)
        //{
        //    Vector2 currentPos = ctx.ReadValue<Vector2>();
        //    Debug.Log($"Swipe currentPos {currentPos}");

        //    Vector2 swipe = currentPos - startPos;

        //    if (swipe.magnitude < minSwipeDistance) return;

        //    if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
        //    {
        //        Debug.Log($"Swipe {swipe}");

        //        if (swipe.x > 0)
        //            TurnRight();
        //        else
        //            TurnLeft();
        //    }

        //    // (Optionnel : reset l’input pour éviter de re-déclencher)
        //    controls.Gameplay.Swipe.Disable();
        //    controls.Gameplay.Swipe.Enable();
        //}

        //private void TurnLeft()
        //{
        //    Debug.Log("Tourner à gauche");
        //    // rotation joueur
        //}

        //private void TurnRight()
        //{
        //    Debug.Log("Tourner à droite");
        //    // rotation joueur
        //}

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"PlayerController trigger {other.tag}");
            if (other.CompareTag("Bonus"))
            {
                // Bonus is managed by the ScoreManager
                scoreManager.StartBonus();

                Rigidbody rb = other.attachedRigidbody;
                if (rb != null)
                {
                    // Direction from player to bonus
                    Vector3 kickDir = (other.transform.position - transform.position).normalized;
                    kickDir.y = 0;
                    // Add a forward + upward impulse (like a foot kick)
                    Vector3 force = kickDir * GetSpeed() * 2f + Vector3.up * 8f;
                    rb.AddForce(force, ForceMode.Impulse);
                    rb.useGravity = true;
                    // Optional: add spin
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
                }
                Destroy(other.gameObject, 3f);
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
            speedMultiplier = 0.5f;
        }

        public void LevelCompleted()
        {
            speedMultiplier = 1f;
            scoreManager.EndBonus();
            HandleMovement(Vector3.zero);
        }

        public void ApplyKnockback(Vector3 direction, float strength)
        {
            knockback = direction.normalized * strength;
        }

        public void ResetPosition()
        {
            speedMultiplier = 0.5f;

            controller.enabled = false;
            Vector3 previous = transform.position;
            Transform start = gameManager.terrainGenerator.StartGO.transform;
            transform.position = start.position;
            transform.rotation = start.rotation;
            currentAngle = 0;
            targetAngle = 0;
            TerrainGenerator.PlaceOnHighestTerrain(transform);
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
            if (enableMovement && gameManager.levelRunning && !gameManager.goalHandler.goalReached)
            {
                // Slowly increase speed multiplier until 10 at 0.1 per second
                speedMultiplier = Mathf.MoveTowards(speedMultiplier, 10f, Time.deltaTime * 0.1f);
                forwardMove = transform.forward * initialSpeed * speedMultiplier;
            }
            HandleInput();
            HandleRotation();
            HandleMovement(forwardMove);

            if (gameManager.gameRunning)
            {
                Vector2Int playerChunk = terrainGenerator.PositionToChunk(transform.position);
                if (playerChunk != currentPlayerChunk)
                {
                    Debug.Log($"Player enters in a chunk: x={transform.position.x} z={transform.position.z} --> playerChunk: {playerChunk}");
                    currentPlayerChunk = playerChunk;
                    terrainGenerator.UpdateChunks(currentPlayerChunk);
                }
            }
        }

        void HandleInput()
        {
            if (gameManager.actionLevel.leftButton.IsHeld || touchEnabler.TurnLeftIsPressed)
            {
                targetAngle -= turnSpeed * Time.deltaTime;
            }
            else if (gameManager.actionLevel.rightButton.IsHeld || touchEnabler.TurnRightIsPressed)
            {
                targetAngle += turnSpeed * Time.deltaTime;
            }
            if (!isJumping && (gameManager.actionLevel.jumpButton.IsHeld || touchEnabler.TurnUpIsPressed))
            {
                verticalVelocity.y = jumpForce;
                isJumping = true;
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
}