using UnityEngine;
using UnityEngine.AI;

public class NavegacionPatrulla : MonoBehaviour
{
    public Transform[] destinos;
    
    [Header("Configuración de Movimiento")]
    public float velocidadPatrulla = 0.5f;
    public float velocidadPersecucion = 0.8f;

    private int indiceActual = -1;
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Perseguir(Vector3 posicion)
    {
        agent.speed = velocidadPersecucion;
        agent.updateRotation = false; // El SensorVision se encarga de rotar hacia el ladrón
        agent.destination = posicion;
    }

    public void Patrullar()
    {
        agent.updateRotation = true;
        if (destinos.Length == 0) return;
        if (agent.remainingDistance < 0.7f && !agent.pathPending)
        {
            indiceActual = (indiceActual + 1) % destinos.Length;
            agent.destination = destinos[indiceActual].position;
        }
    }
}