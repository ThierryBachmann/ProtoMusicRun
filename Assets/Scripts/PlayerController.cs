/*
 * Prototype Unity 3D : Sc�ne "for�t musicale"
 * Objectifs :
 * - G�n�ration dynamique de terrain
 * - Joueur en vue subjective
 * - Synchronisation musique MIDI avec Maestro MPTK
 * - Obstacles + syst�me de score
 */

// === PlayerController.cs ===
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
    public float turnSpeed = 90f; // vitesse d'interpolation de rotation
    public float maxAngle = 45f;
    private float currentAngle = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        //isGrounded = controller.isGrounded;
        //if (isGrounded && velocity.y < 0) velocity.y = -2f;

        //float moveX = Input.GetAxis("Horizontal") * directionSpeed;
        //Vector3 move = transform.right * moveX + transform.forward * baseSpeed * speedMultiplier;
        //controller.Move(move * Time.deltaTime);

        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        //{
        //    velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        //}

        //velocity.y += gravity * Time.deltaTime;
        //controller.Move(velocity * Time.deltaTime);
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            currentAngle -= turnSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            currentAngle += turnSpeed * Time.deltaTime;
        }

        currentAngle = Mathf.Clamp(currentAngle, -maxAngle, maxAngle);

        // Tourne le joueur (et donc la cam�ra avec lui)
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