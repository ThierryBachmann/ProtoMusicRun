/*
 * Prototype Unity 3D : Scène "forêt musicale"
 * Objectifs :
 * - Génération dynamique de terrain
 * - Joueur en vue subjective
 * - Synchronisation musique MIDI avec Maestro MPTK
 * - Obstacles + système de score
 */

// === PlayerController.cs ===
using Mono.Cecil.Cil;
using System.Collections;
using UnityEngine;
using static Unity.Burst.Intrinsics.Arm;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public GoalHandler goalHandler;

    [Header("Mouvement avant")]
    public float speedMultiplier = 1f;
    public float moveSpeed = 5f;

    [Header("Orientation")]
    public float turnSpeed = 90f; // vitesse de rotation fluide en °/s
    public float maxAngle = 45f;

    [Header("Saut & gravité")]
    private float gravity = 9.81f;
    public float jumpForce = 5f;

    [Header("Knock‑back")]
    public float knockbackDecay = 4f;  // plus grand = ralentit plus vite


    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 verticalVelocity;  // stocke la composante Y du saut/gravité
    private float targetAngle = 0f; // angle actuel utilisé pour la rotation
    private float currentAngle = 0f;
    private Vector3 knockback = Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void ApplyKnockback(Vector3 direction, float strength)
    {
        knockback = direction.normalized * strength;
    }

    void Update()
    {
        if (!goalHandler.goalReached)
        {
            HandleInput();
            HandleRotation();
            HandleMovement();

            // Remontée progressive du multiplicateur de vitesse
            // Il rapproche doucement speedMultiplier de 10f a une vitesse de 0.5 par seconde.
            // Tu peux ajuster ce 0.5f selon la douceur voulue(par ex. 1f = plus rapide, 0.2f = plus lent)
            speedMultiplier = Mathf.MoveTowards(speedMultiplier, 10f, Time.deltaTime * 0.1f);
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
    void HandleMovement()
    {

        // Avance “normale”
        Vector3 forwardMove = transform.forward * moveSpeed * speedMultiplier;

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

    public float GetSpeed() => moveSpeed * speedMultiplier;
}