using UnityEngine;

public class Jugador : MonoBehaviour
{
    public float walkSpeed = 1f;
    public float runSpeed = 3f;
    public float rotationSpeed = 15f;
    
    public Transform camara;
    
    private Rigidbody rb;
    private Animator anim;
    private Vector3 inputMovement;
    private float currentSpeed;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        if (camara == null) camara = Camera.main.transform;
        
        // Para que no haya tirones con Rigidbody
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector3 camForward = camara.forward;
        Vector3 camRight = camara.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Detecta si el jugador esta pulsando shift
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // Velocidad actual
        currentSpeed = isRunning ? runSpeed : walkSpeed;

        inputMovement = (camForward * moveVertical) + (camRight * moveHorizontal);

        // Actualizar animaciones
        if (anim != null) 
        {
            float valorAnimacion = 0; // 0 para idle, 1 para caminar, 2 para correr
            if (inputMovement.magnitude > 0.1f)
            {
                valorAnimacion = isRunning ? 2f : 1f;
            }
            float suavizado = 10f;
            float currentAnimSpeed = anim.GetFloat("Speed");
            anim.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, valorAnimacion, Time.deltaTime * suavizado));
        }
    }

    private void FixedUpdate()
    {
        // Si el input es significativo, mover y rotar al jugador
        if (inputMovement.magnitude > 0.1f)
        {
            if (inputMovement.magnitude > 1) inputMovement.Normalize();

            // Rotación suave
            Quaternion targetRotation = Quaternion.LookRotation(inputMovement);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

            // Movimiento físico
            rb.linearVelocity = new Vector3(inputMovement.x * currentSpeed, rb.linearVelocity.y, inputMovement.z * currentSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }
}