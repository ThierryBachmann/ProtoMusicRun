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

namespace MusicRun
{
    [RequireComponent(typeof(CharacterController))]
    public class TestPlayer : MonoBehaviour
    {
        [Header("Debug")]
        public bool enableMovement = true;
        public bool isJumping;
        public Vector3 verticalVelocity;
        public float targetAngle = 0f;
        public float currentAngle = 0f;
        public Vector3 knockback = Vector3.zero;

        [Header("Orientation")]
        public float turnSpeed = 90f;
        public float maxAngle = 45f;

        [Header("Jump")]
        public float gravity = 12.81f;
        public float jumpForce;



        private GameManager gameManager;
        public DateTime timeStartLevel;


        void Awake()
        {
            gameManager = Utilities.FindGameManager();
            if (gameManager == null)
                return;
        }

        void Update()
        {
            if (enableMovement)
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
            if (!isJumping && (Input.GetKeyDown(KeyCode.Space)))
            {
                verticalVelocity.y = jumpForce;
                isJumping = true;
            }
        }

        void HandleRotation()
        {
            //Smooth interpolation between the current angle and the target angle, with an adjustable smoothing factor.
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * 5f);
            transform.rotation = Quaternion.Euler(0, currentAngle, 0);
        }
    }
}