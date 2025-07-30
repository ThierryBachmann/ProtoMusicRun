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
using System.Collections.Generic;
using UnityEngine;

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
        private float gravity = 9.81f;
        public float jumpForce;

        [Header("Knock‑back")]
        public float knockbackDecay = 4f;

        [Header("Score")]
        public int playerPosition = 99999;
        public long playerBestScore = 0;
        public long playerLastScore = 0;


        private CharacterController controller;
        [Header("GameObject")]
        public GameManager gameManager;
        public DateTime timeStartLevel;


        void Awake()
        {

        }

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }


        public void LevelStarted()
        {
            ResetPosition();
            timeStartLevel = DateTime.Now;
            speedMultiplier = 0.5f;
        }

        public void LevelCompleted()
        {
            speedMultiplier = Mathf.MoveTowards(speedMultiplier, 0f, Time.deltaTime * 15f);

            HandleMovement(Vector3.zero);
        }

        public void ApplyKnockback(Vector3 direction, float strength)
        {
            knockback = direction.normalized * strength;
        }

        public void ResetPosition()
        {
            speedMultiplier = 0.5f;

            CharacterController cc = GetComponent<CharacterController>();
            cc.enabled = false;
            Transform start = gameManager.terrainGenerator.StartGO.transform;
            Debug.Log($"Player ResetPosition {start.position}");
            transform.position = start.position;
            transform.rotation = start.rotation;
            currentAngle = 0;
            targetAngle = 0;
            cc.enabled = true;
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
            if (!isJumping && Input.GetKeyDown(KeyCode.Space) || transform.position.y < 0f)
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
}