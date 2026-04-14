using UnityEngine;
using UnityEngine.AI;

public class SincronizadorAnimacion : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>(); 
    }

    void Update() 
    {
        // Pasa la velocidad real a la que se mueve el guardia a tu Animator
        if (anim != null && agent != null) 
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }
}