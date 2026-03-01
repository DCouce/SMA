using UnityEngine;
using UnityEngine.AI;

public class Revisar : MonoBehaviour
{
    private NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void EjecutarRevisar(Vector3 posicion)
    {
        // 1. Ir al sitio
        agent.destination = posicion;
        agent.speed = 0.8f; // Va rápido porque lo acaba de perder

        // 2. Si ha llegado, empieza a girar
        if (!agent.pathPending && agent.remainingDistance < 0.2f)
        {            
            // Giro suave de izquierda a derecha
            float angulo = Mathf.Sin(Time.time * 3f) * 50f; 
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + angulo * Time.deltaTime, 0);
        }
    }

}
