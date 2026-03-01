using UnityEngine;
using UnityEngine.AI;

public class NavegacionPatrulla : MonoBehaviour
{
    public Transform[] destinos;
    private int indiceActual = -1;
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void Perseguir(Vector3 posicion)
    {
        agent.destination = posicion;
    }

    public void Patrullar()
    {
        if (destinos.Length == 0) return;
        if (agent.remainingDistance < 0.7f && !agent.pathPending)
        {
            indiceActual = (indiceActual + 1) % destinos.Length;
            agent.destination = destinos[indiceActual].position;
        }
    }
}