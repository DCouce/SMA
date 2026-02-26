using UnityEngine;

public class Audio : MonoBehaviour
{
    public AudioSource audioSource;
    
    public AudioClip[] walkClips;    
    public AudioClip[] runClips;   
    
    public float walkVolume = 0.4f;
    public float runVolume = 0.8f;

    public float noiseLevel = 0f;

    private PlayerController player;
    public float baseDetectionRange = 5f;
    public LayerMask capaGuardias;

    void Start()
    {
        player = GetComponent<PlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void OnFootstep()
    {
        if (!player.IsGrounded()) return;
        if (!player.IsMoving()) return; 

        if (player.IsRunning())
        {
            PlayRandomFromList(runClips, runVolume);
            noiseLevel = 1.0f;
            NotifyAgents();
        }
        else
        {
            PlayRandomFromList(walkClips, walkVolume);
            noiseLevel = 0.4f;
            NotifyAgents();
        }
    }

    private void PlayRandomFromList(AudioClip[] list, float volume)
    {
        if (list.Length > 0 && audioSource != null)
        {
            int index = Random.Range(0, list.Length);
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(list[index], volume);
        }
    }
    
    void Update()
    {
        if (noiseLevel > 0) 
            noiseLevel -= Time.deltaTime * 2f;
    }

    private void NotifyAgents()
    {
        float finalRange = baseDetectionRange * noiseLevel;
        Collider[] closeObjects = Physics.OverlapSphere(transform.position, finalRange, capaGuardias);

        foreach (Collider obj in closeObjects)
        {
            Guardia scriptGuardia = obj.GetComponent<Guardia>();
            if (scriptGuardia != null)
            {
                scriptGuardia.OnHeardSound(transform.position);
            }
        }
    }
}