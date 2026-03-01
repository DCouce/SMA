using UnityEngine;
using UnityEngine.AI;

public class Audio : MonoBehaviour
{
    public AudioSource audioSource;
    
    [Header("Sonidos (Opcional para Guardias)")]
    public AudioClip[] walkClips;    
    public AudioClip[] runClips;   
    public float walkVolume = 0.4f;
    public float runVolume = 0.8f;

    [Header("Configuración de Ruido")]
    public float baseDetectionRange = 5f;
    public LayerMask capaQueEscucha; // Capas de guardias
    public float noiseLevel = 0f;

    // Referencias automáticas
    private PlayerController player;
    private NavMeshAgent agent; 

    void Start()
    {
        player = GetComponent<PlayerController>();
        agent = GetComponent<NavMeshAgent>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void OnFootstep()
    {
        bool esJugador = (player != null);
        bool moviendose = false;
        bool corriendo = false;

        // 1. Detectar movimiento según quién sea
        if (esJugador)
        {
            if (!player.IsGrounded() || !player.IsMoving()) return;
            moviendose = true;
            corriendo = player.IsRunning();
        }
        else if (agent != null) // Es un Guardia
        {
            if (agent.velocity.magnitude < 0.1f) return;
            moviendose = true;
            corriendo = agent.velocity.magnitude > 1.5f; 
        }

        if (!moviendose) return;

        // 2. Ejecutar Sonido Físico (Solo si hay clips y AudioSource)
        // Para los guardias, puedes dejar los clips vacíos en el Inspector y no sonará nada.
        float volumenActual = corriendo ? runVolume : walkVolume;
        AudioClip[] clipsActuales = corriendo ? runClips : walkClips;

        if (clipsActuales.Length > 0)
        {
            PlayRandomFromList(clipsActuales, volumenActual);
        }

        // 3. Notificar al mundo (Ruido Interno)
        noiseLevel = corriendo ? 1.0f : 0.4f;
        NotifyAgents();
    }

    private void PlayRandomFromList(AudioClip[] list, float volume)
    {
        if (audioSource != null && list.Length > 0)
        {
            int index = Random.Range(0, list.Length);
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(list[index], volume);
        }
    }
    
    void Update()
    {
        // El nivel de ruido se disipa con el tiempo
        if (noiseLevel > 0) 
            noiseLevel -= Time.deltaTime * 2f;
    }

    private void NotifyAgents()
    {
        float finalRange = baseDetectionRange * noiseLevel;
        Collider[] closeObjects = Physics.OverlapSphere(transform.position, finalRange, capaQueEscucha);

        foreach (Collider obj in closeObjects)
        {
            // Evitar que el guardia se escuche a sí mismo
            if (obj.gameObject == this.gameObject) continue;

            // Si es un Guardia, activa su oído
            if (obj.TryGetComponent<Oido>(out Oido oido))
            {
                oido.OnHeardSound(transform.position);
            }
        }
    }
}