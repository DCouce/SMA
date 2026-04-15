using UnityEngine;
using UnityEngine.AI;

public class NavegacionPatrulla : MonoBehaviour
{
    public Transform[] destinos_sin_robar;
    public Transform[] destinos_robado;
    
    [Header("Configuración de Movimiento")]
    public float velocidadPatrulla = 0.5f;
    public float velocidadAlerta = 0.75f;

    private int indiceActual = 0;
    private NavMeshAgent agent;
    private Modelado modelo;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        modelo = GetComponent<Modelado>();
    }

    // Al encenderse (cuando el Control lo decide), busca el punto más cercano
    void OnEnable()
    {   
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            indiceActual = ObtenerIndiceMasCercano();
            ActualizarDestino();
        }
    }

    void Update()
    {
        Transform[] destinosActuales = modelo.sabeRobado ? destinos_robado : destinos_sin_robar;
        if (destinosActuales.Length == 0) return;

        agent.speed = modelo.sabeRobado ? velocidadAlerta : velocidadPatrulla;

        // Lógica de patrulla automática
        if (!agent.pathPending && agent.remainingDistance < 0.7f)
        {
            indiceActual = (indiceActual + 1) % destinosActuales.Length;
            ActualizarDestino();
        }
    }

    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
    }

    private void ActualizarDestino()
    {
        Transform[] destinosActuales = modelo.sabeRobado ? destinos_robado : destinos_sin_robar;
        if (destinosActuales.Length > 0) agent.destination = destinosActuales[indiceActual].position;
    }

    private int ObtenerIndiceMasCercano()
    {
        Transform[] destinosActuales = modelo.sabeRobado ? destinos_robado : destinos_sin_robar;
        int mejorIndice = 0;
        float distanciaMinima = Mathf.Infinity;
        for (int i = 0; i < destinosActuales.Length; i++)
        {
            float distancia = Vector3.Distance(transform.position, destinosActuales[i].position);
            if (distancia < distanciaMinima) { distanciaMinima = distancia; mejorIndice = i; }
        }
        return mejorIndice;
    }
}