using UnityEngine;

public class Audio : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip jumpSound;
    
    public AudioClip[] walkClips;    
    public AudioClip[] runClips;   
    public AudioClip[] landingClips; 
    
    public float walkVolume = 0.4f;
    public float runVolume = 0.8f;

    public float noiseLevel = 0f;

    private PlayerController player;

    void Start()
    {
        player = GetComponent<PlayerController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void PlayJumpSound()
    {
        PlaySound(jumpSound, 1.0f);
        noiseLevel = 2.0f;
    }

    public void OnFootstep()
    {
        if (!player.IsGrounded()) return;

        if (!player.IsMoving()) return; 

        if (player.IsRunning())
        {
            PlayRandomFromList(runClips, runVolume);
            noiseLevel = 1.0f;
        }
        else
        {
            PlayRandomFromList(walkClips, walkVolume);
            noiseLevel = 0.4f;
        }
    }

    public void OnLanding()
    {
        PlayRandomFromList(landingClips, runVolume + 0.2f);
        noiseLevel = 2.0f;
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

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null) audioSource.PlayOneShot(clip, volume);
    }
    
    void Update()
    {
        if (noiseLevel > 0) noiseLevel -= Time.deltaTime * 2f;
    }
}