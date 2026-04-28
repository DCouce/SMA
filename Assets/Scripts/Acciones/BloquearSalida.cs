using UnityEngine;
using UnityEngine.AI;

// Acción de planificación: el guardia navega a un punto de salida y espera allí.
// La capa reactiva puede interrumpirla (perseguir tiene prioridad mayor).
// Cuando llega a posición, queda parado y notifica al gestor con InformDone.
public class BloquearSalida : MonoBehaviour
{
    private NavMeshAgent agent;
    private Comunicacion comms;

    private bool enPosicion = false;
    private Vector3 puntoGuardia;

    // Contexto de la conversación FIPA que activó esta tarea
    private Comunicacion gestorActual;
    private string convIdActual;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        comms = GetComponent<Comunicacion>();
    }

    // Llamado desde Comunicacion al recibir un AcceptProposal.
    // Recibe el punto a cubrir y el contexto de la conversación para
    // poder enviar el InformDone al completar.
    public void SetPunto(Vector3 punto, Comunicacion gestor = null, string convId = null)
    {
        puntoGuardia = punto;
        gestorActual = gestor;
        convIdActual = convId;
        enPosicion   = false;

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

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.4f)
        {
            enPosicion = true;
            agent.isStopped = true;
            Debug.Log($"<color=blue>[BLOQUEO]</color> {gameObject.name} en posición de guardia en {puntoGuardia}.");

            // Notificar al gestor que hemos completado la tarea
            if (gestorActual != null && comms != null)
            {
                MensajeFIPA informDone = new MensajeFIPA(
                    FIPAPerformativa.InformDone,
                    comms,
                    $"(inform-done (action {gameObject.name} " +
                    $"(ir-a (coord {puntoGuardia.x:F1} {puntoGuardia.y:F1} {puntoGuardia.z:F1}))))",
                    convIdActual,
                    "fipa-contract-net");

                comms.Enviar(gestorActual, informDone);
            }
        }
    }

    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        enPosicion = false;
    }
}