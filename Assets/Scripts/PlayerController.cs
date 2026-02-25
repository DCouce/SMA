using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public float walkSpeed = 0.5f; 
    public float runSpeed = 1f;
    public float jumpHeight = 0.25f;
    public float gravity = -9.81f; 
    
    public float jumpForwardBoost = 0.75f; 

    private CharacterController controller;
    private Animator anim;
    private Audio audioManager; 

    private Vector3 movementInput;
    private Vector3 velocity;      
    private Vector3 airInertia;   
    private bool isRunning;

    [Header("Robo")]
    public LayerMask capaCuadro;
    public Transform puntoMochila;
    public GameObject textoE;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        audioManager = GetComponent<Audio>();
        
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    public void Move(InputAction.CallbackContext context) => movementInput = context.ReadValue<Vector2>();
    public void Run(InputAction.CallbackContext context) => isRunning = context.ReadValueAsButton();

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Obtenemos la dirección del input actual
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            forward.y = 0; 
            right.y = 0;

            forward.Normalize(); 
            right.Normalize();
            
            Vector3 inputDir = forward * movementInput.y + right * movementInput.x;

            if (inputDir.sqrMagnitude > 0.01f)
            {
                // Si se mueve, salta hacia donde apunta
                airInertia = inputDir * jumpForwardBoost;
            }
            else 
            {
                // Si esta quieto, salta hacia donde mira el personaje
                airInertia = transform.forward * jumpForwardBoost; 
            }

            if (anim != null) anim.SetTrigger("Jump");
            if (audioManager != null) audioManager.PlayJumpSound();
        }
    }

    void Update()
    {
        Vector3 finalMove = Vector3.zero;

        // Movimiento por teclado
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0; 
        right.y = 0;

        forward.Normalize(); 
        right.Normalize();

        Vector3 moveInputDirection = forward * movementInput.y + right * movementInput.x;

        if (controller.isGrounded)
        {
            // En el suelo, movimiento normal
            float speed = isRunning ? runSpeed : walkSpeed;
            finalMove = moveInputDirection * speed;

            // Limpiamos la inercia para que no se deslice al caer
            if (velocity.y < 0) 
            {
                velocity.y = -2f;
                airInertia = Vector3.zero; 
            }
        }
        else
        {
            // En el aire, usamos la inercia que capturamos en el momento del salto
            finalMove = airInertia;
        }

        // Aplicamos gravedad
        velocity.y += gravity * Time.deltaTime;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);

        if (moveInputDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveInputDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 12f * Time.deltaTime);
        }

        UpdateAnimator();

        Collider[] hit = Physics.OverlapSphere(transform.position, 0.5f, capaCuadro);
        bool cerca = hit.Length > 0;

        textoE.SetActive(cerca);

        if (cerca && Input.GetKeyDown(KeyCode.E)) {
            if (hit[0].TryGetComponent<Cuadro>(out Cuadro cuadro)) {
                cuadro.SerRobado(puntoMochila);
            }
        }    
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;
        anim.SetBool("isGrounded", controller.isGrounded);
        float target = (movementInput.magnitude > 0.1f) ? (isRunning ? 2f : 1f) : 0f;
        anim.SetFloat("Speed", Mathf.Lerp(anim.GetFloat("Speed"), target, Time.deltaTime * 10f));
    }

    public bool IsGrounded() => controller.isGrounded;
    public bool IsMoving() => movementInput.sqrMagnitude > 0.01f;
    public bool IsRunning() => isRunning && movementInput.sqrMagnitude > 0.01f;
}