using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    public Transform cameraTransform;
    public float jumpHeight = 0.25f;
    public float gravity = -9.81f;
    public bool shouldFaceMovementDirection = false;
    public float walkSpeed = 0.5f;
    public float runSpeed = 1f;

    private CharacterController controller;
    private Vector3 movementInput;
    private Vector3 velocity;
    private Animator anim;
    private bool isRunning;


    void Start()
{
    controller = GetComponent<CharacterController>();
    anim = GetComponentInChildren<Animator>();
}

    public void Move(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
        Debug.Log($"Movement input: {movementInput}");
    }

    public void Jump(InputAction.CallbackContext context)
    {
        Debug.Log($"Jumping: {context.performed} - Is Grounde: {controller.isGrounded}");
        if (context.performed && controller.isGrounded)
        {
            Debug.Log("We are suppose tu jump");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    public void Run(InputAction.CallbackContext context)
    {
        isRunning = context.ReadValueAsButton();
    }
    void Update()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * movementInput.y + right * movementInput.x;
        
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (shouldFaceMovementDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 10f * Time.deltaTime);
        }
        
        if (anim != null)
        {
            float valorAnimacion = 0f;

            if (moveDirection.magnitude > 0.1f)
            {
                valorAnimacion = isRunning ? 2f : 1f;
            }

            float suavizado = 10f;
            float currentAnimSpeed = anim.GetFloat("Speed");

            anim.SetFloat("Speed",
                Mathf.Lerp(currentAnimSpeed, valorAnimacion, Time.deltaTime * suavizado));
        }

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
