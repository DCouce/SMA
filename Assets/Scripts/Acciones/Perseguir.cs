using UnityEngine;
using UnityEngine.AI;

public class Perseguir : MonoBehaviour
{
    
    private NavMeshAgent agent;
    public float velocidadPersecucion = 0.8f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void EjecutarPersecucion(Vector3 posicion)
    {
        agent.speed = velocidadPersecucion;
        agent.destination = posicion;
    }

}