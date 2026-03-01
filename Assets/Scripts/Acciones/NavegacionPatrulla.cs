using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class NavegacionPatrulla : MonoBehaviour
{
    public Transform[] destinos_sin_robar; // Para cuando no sabe que ha sido robado
    public Transform[] destinos_robado; // Para cuando sabe que sí ya ha sido robado
    
    [Header("Configuración de Movimiento")]
    public float velocidadPatrulla = 0.5f;
    public float velocidadAlerta = 0.75f; // Si sabe que lo han robado

    private int indiceActual = 0; // Empezamos en 0 por defecto
    private NavMeshAgent agent;
    private Guardia guardia;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        guardia = GetComponent<Guardia>();
    }

    void Start()
    {
        if (destinos_sin_robar.Length > 0 || destinos_robado.Length > 0)
        {
            // Buscamos cuál es el índice del punto más cercano
            indiceActual = ObtenerIndiceMasCercano();
            
            // Le decimos al agente que vaya a ese punto primero
            if (guardia.sabeRobado == false){
                agent.destination = destinos_sin_robar[indiceActual].position;
            }
            else
            {
                agent.destination = destinos_robado[indiceActual].position;

            }
            agent.speed = velocidadPatrulla;

        }
    }

    private int ObtenerIndiceMasCercano()
    {
        Transform[] destinosActuales = guardia.sabeRobado ? destinos_robado : destinos_sin_robar;

        int mejorIndice = 0;
        float distanciaMinima = Mathf.Infinity;

        for (int i = 0; i < destinosActuales.Length; i++)
        {
            float distancia = Vector3.Distance(transform.position, destinosActuales[i].position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                mejorIndice = i;
            }
        }
        return mejorIndice;
    }

    public void Patrullar()
    {
        Transform[] destinosActuales = guardia.sabeRobado ? destinos_robado : destinos_sin_robar;

        if (destinosActuales.Length == 0) return;

        agent.updateRotation = true;
        agent.speed = guardia.sabeRobado ? velocidadAlerta : velocidadPatrulla;

        // Si el agente está cerca del destino actual y no está calculando ruta...
        if (!agent.pathPending && agent.remainingDistance < 0.7f)
        {

            indiceActual = (indiceActual + 1) % destinosActuales.Length;
            
            agent.destination = destinosActuales[indiceActual].position;
        }

    }
}