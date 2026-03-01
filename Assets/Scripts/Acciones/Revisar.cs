using UnityEngine;
using UnityEngine.AI;

public class Revisar : MonoBehaviour
{
    private NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void EjecutarRevisar(Vector3 posicion)
    {
        // Ir al sitio
        agent.destination = posicion;
        agent.speed = 0.8f; // Va rápido porque lo acaba de perder

        // Si ha llegado, empieza a girar
        if (!agent.pathPending && agent.remainingDistance < 0.2f)
        {   
            transform.Rotate(Vector3.up, 100f * Time.deltaTime);   
        }
    }

}
