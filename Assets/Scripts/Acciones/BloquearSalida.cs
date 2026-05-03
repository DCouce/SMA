using UnityEngine;
using UnityEngine.AI;

public class BloquearSalida : MonoBehaviour
{
    private NavMeshAgent agent;
    private Mensajeria   comms;

    private bool    enPosicion   = false;
    private Vector3 puntoGuardia;
    private float   tSiguienteDestino;

    private Mensajeria gestorActual;
    private string     convIdActual;

    private const float RADIO_PATRULLA = 1.5f;
    private const float INTERVALO_PASO = 3f;

    public string ConversacionActual() => convIdActual;
    public bool EstaActivo () => !string.IsNullOrEmpty(convIdActual);
    public Vector3 PuntoActual() => puntoGuardia;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        comms = GetComponent<Mensajeria>();
    }

    public void SetPunto(Vector3 punto, Mensajeria gestor = null,
                         string convId = null, string nombreZona = null)
    {
        puntoGuardia = punto;
        gestorActual = gestor;
        convIdActual = convId;
        enPosicion   = false;
    }

    public void LimpiarContrato()
    {
        gestorActual = null;
        convIdActual = null;
        // No tocamos puntoGuardia, no hace falta
    }
    void OnEnable()
    {
        enPosicion = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed     = 0.8f;
            agent.SetDestination(puntoGuardia);
        }
    }


    void Update()
    {
        if (!enPosicion)
        {
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.4f)
            {
                enPosicion        = true;
                tSiguienteDestino = Time.time + INTERVALO_PASO;

                Debug.Log($"<color=blue>[BLOQUEO]</color> {gameObject.name} en posición en {puntoGuardia}.");
                EnviarInformDone();
            }
            return;
        }

        // Patrulla en radio pequeño alrededor del punto de bloqueo
        bool llegó = !agent.pathPending && agent.remainingDistance < 0.3f;
        if (llegó && Time.time >= tSiguienteDestino)
        {
            agent.isStopped = false;
            agent.SetDestination(PuntoEnRadio());
            tSiguienteDestino = Time.time + INTERVALO_PASO;
        }
    }

    private Vector3 PuntoEnRadio()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset    = Random.insideUnitCircle * RADIO_PATRULLA;
            Vector3 candidato = puntoGuardia + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, RADIO_PATRULLA, NavMesh.AllAreas))
                return hit.position;
        }
        return puntoGuardia;
    }

    private void EnviarInformDone()
    {
        if (gestorActual == null || comms == null) return;

        comms.Enviar(gestorActual, new MensajeFIPA(
            "inform-done", comms,
            MensajeFIPA.ContentVec3(puntoGuardia),
            convIdActual,
            "fipa-contract-net"));
    }

        
    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        enPosicion = false;
    }
}
