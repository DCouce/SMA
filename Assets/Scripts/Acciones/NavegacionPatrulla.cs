using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class NavegacionPatrulla : MonoBehaviour
{
    public Transform[] destinos_sin_robar;
    public Transform[] destinos_robado;
    
    [Header("Configuración de Movimiento")]
    public float velocidadPatrulla = 0.5f;
    public float velocidadPersecucion = 0.8f;

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
            // 1. Buscamos cuál es el índice del punto más cercano
            indiceActual = ObtenerIndiceMasCercano();
            
            // 2. Le decimos al agente que vaya a ese punto primero
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
        int mejorIndice = 0;
        float distanciaMinima = Mathf.Infinity;
        if (guardia.sabeRobado == false){

        for (int i = 0; i < destinos_sin_robar.Length; i++)
        {
            float distancia = Vector3.Distance(transform.position, destinos_sin_robar[i].position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                mejorIndice = i;
            }
        }
        return mejorIndice;
        }
        else
        {
           for (int i = 0; i < destinos_robado.Length; i++)
        {
            float distancia = Vector3.Distance(transform.position, destinos_robado[i].position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                mejorIndice = i;
            }
        }
        return mejorIndice; 
        }
    }

    public void Patrullar()
    {
        if (guardia.sabeRobado == false){
        if (destinos_sin_robar.Length == 0) return;

        agent.updateRotation = true;
        agent.speed = velocidadPatrulla;

        // Si el agente está cerca del destino actual y no está calculando ruta...
        if (!agent.pathPending && agent.remainingDistance < 0.7f)
        {
            // AVANCE LÓGICO: Simplemente incrementamos el índice. 
            // Si el más cercano fue el 3, el siguiente será el 4, luego el 5... 
            // y al llegar al final del array volverá al 0 gracias al operador % (módulo).
            indiceActual = (indiceActual + 1) % destinos_sin_robar.Length;
            
            agent.destination = destinos_sin_robar[indiceActual].position;
        }
        }
        else
        {
        if (destinos_robado.Length == 0) return;

        agent.updateRotation = true;
        agent.speed = velocidadPatrulla;

        // Si el agente está cerca del destino actual y no está calculando ruta...
        if (!agent.pathPending && agent.remainingDistance < 0.7f)
        {
            // AVANCE LÓGICO: Simplemente incrementamos el índice. 
            // Si el más cercano fue el 3, el siguiente será el 4, luego el 5... 
            // y al llegar al final del array volverá al 0 gracias al operador % (módulo).
            indiceActual = (indiceActual + 1) % destinos_robado.Length;
            
            agent.destination = destinos_robado[indiceActual].position;
        } 
        }

    }

    public void Perseguir(Vector3 posicion)
    {
        agent.speed = velocidadPersecucion;
        agent.destination = posicion;
    }
}