using UnityEngine;
using UnityEngine.AI;

// Acción de planificación: el guardia navega a un punto de salida y espera allí.
// La capa reactiva puede interrumpirla (perseguir tiene prioridad 2 > 1).
// Cuando está en posición, el agente queda parado; SensorVision maneja la intercepción si el ladrón aparece.
public class BloquearSalida : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool enPosicion = false;
    private Vector3 puntoGuardia;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    // CapaPlanificacion llama a esto ANTES de proponer la acción
    public void SetPunto(Vector3 punto)
    {
        puntoGuardia = punto;
        enPosicion = false;
        if (this.enabled && agent != null && agent.isOnNavMesh)
            agent.SetDestination(puntoGuardia);
    }

    void OnEnable()
    {
        enPosicion = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = 0.8f;
            agent.SetDestination(puntoGuardia);
        }
    }

    void Update()
    {
        if (enPosicion) return;

        if (!agent.pathPending && agent.remainingDistance < 0.4f)
        {
            enPosicion = true;
            agent.isStopped = true;
            Debug.Log($"<color=blue>[BLOQUEO]</color> {gameObject.name} en posición de guardia en {puntoGuardia}.");
        }
    }

    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        enPosicion = false;
    }
}
