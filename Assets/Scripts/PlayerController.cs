/*
 * Prototype Unity 3D : Scène "forêt musicale"
 * Objectifs :
 * - Génération dynamique de terrain
 * - Joueur en vue subjective
 * - Synchronisation musique MIDI avec Maestro MPTK
 * - Obstacles + système de score
 */

// === PlayerController.cs ===
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float baseSpeed = 5f;
    public float speedMultiplier = 1f;
    public float directionSpeed = 3f;
    public float jumpForce = 5f;
    private CharacterController controller;
    private Vector3 velocity;
    private float gravity = 9.81f;
    private bool isGrounded;
    public float turnSpeed = 90f; // vitesse de rotation fluide en °/s
    public float maxAngle = 45f;
    private float targetAngle = 0f; // angle actuel utilisé pour la rotation
    private float currentAngle = 0f;
    private bool isBeingPushed = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
    public void ForceMove(Vector3 offset)
    {
        StartCoroutine(ApplyPush(offset));
    }
    private IEnumerator ApplyPush(Vector3 offset)
    {
        isBeingPushed = true;
        controller.Move(offset);
        yield return new WaitForSeconds(0.5f); // Délai avant de réactiver les collisions normales
        isBeingPushed = false;
    }

    public bool IsBeingPushed()
    {
        return isBeingPushed;
    }
    void Update()
    {
        if (isBeingPushed) return;

        HandleInput();
        HandleRotation();
        HandleMovement();
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
        Vector3 move = transform.forward * baseSpeed;

        isGrounded = controller.isGrounded;
        if (isGrounded)
        {
            velocity.y = -1f;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpForce;
            }
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        move.y = velocity.y;
        controller.Move(move * Time.deltaTime);
    }

    public float GetSpeed() => baseSpeed * speedMultiplier;
}