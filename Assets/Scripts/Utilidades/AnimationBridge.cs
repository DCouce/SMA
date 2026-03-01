using UnityEngine;

public class AnimationBridge : MonoBehaviour
{
    private Audio audioManager;

    void Start()
    {
        audioManager = GetComponentInParent<Audio>();
    }

    public void OnFootstep() => audioManager?.OnFootstep();
}