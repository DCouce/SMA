using UnityEngine;
using UnityEngine.AI;

public class Perseguir : MonoBehaviour
{
    private NavMeshAgent agent;
    public float velocidadPersecucion = 0.8f;
    private Vector3 ultimaPosicionDeteccion;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    // La Capa Reactiva llama a esto constantemente
    public void ActualizarObjetivo(Vector3 posicion)
    {
        ultimaPosicionDeteccion = posicion;
        if (this.enabled && agent.isOnNavMesh) agent.SetDestination(posicion);
    }

    void OnEnable()
    {
        agent.speed = velocidadPersecucion;
        agent.isStopped = false;
        agent.SetDestination(ultimaPosicionDeteccion);
    }

    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
    }
}