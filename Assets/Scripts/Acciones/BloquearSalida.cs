using UnityEngine;
using UnityEngine.AI;

public class BloquearSalida : MonoBehaviour
{
    private NavMeshAgent agent;
    private Mensajeria comms;

    private bool    enPosicion       = false;
    private bool    buscandoEnZona   = false;
    private float   tSiguienteDestino;
    private Vector3 puntoGuardia;
    private string  zonaNombre;
    private Zona    zonaCache;

    private Mensajeria gestorActual;
    private string       convIdActual;

    private const float INTERVALO_BUSQUEDA = 4f;
    public string ConversacionActual() => convIdActual;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        comms = GetComponent<Mensajeria>();
    }

    public void SetPunto(Vector3 punto, Mensajeria gestor = null,
                         string convId = null, string nombreZona = null)
    {
        puntoGuardia    = punto;
        gestorActual    = gestor;
        convIdActual    = convId;
        zonaNombre      = nombreZona;
        zonaCache       = !string.IsNullOrEmpty(nombreZona)
                            ? GestorZonas.Instance?.ObtenerZonaPorNombre(nombreZona)
                            : null;
        enPosicion      = false;
        buscandoEnZona  = false;

        if (this.enabled && agent != null && agent.isOnNavMesh)
            agent.SetDestination(puntoGuardia);
    }

    void OnEnable()
    {
        enPosicion     = false;
        buscandoEnZona = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed     = 0.8f;
            agent.SetDestination(puntoGuardia);
        }
    }

    void Update()
    {
        // Fase 1: ir al punto de bloqueo
        if (!enPosicion)
        {
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.4f)
            {
                enPosicion      = true;
                agent.isStopped = true;

                Debug.Log($"<color=blue>[BLOQUEO]</color> {gameObject.name} en posición en {puntoGuardia}.");

                EnviarInformDone();

                // Pasamos a buscar dentro de la zona si la conocemos
                if (zonaCache != null)
                {
                    buscandoEnZona     = true;
                    tSiguienteDestino  = Time.time + 0.5f;
                }
            }
            return;
        }

        // Fase 2: rondas dentro de la zona buscando al ladrón
        if (buscandoEnZona && zonaCache != null)
        {
            bool llegoAlDestino = !agent.pathPending && agent.remainingDistance < 0.5f;
            if (llegoAlDestino && Time.time >= tSiguienteDestino)
            {
                Vector3 nuevoDestino = ElegirPuntoAleatorioEnZona();
                agent.isStopped = false;
                agent.SetDestination(nuevoDestino);
                tSiguienteDestino = Time.time + INTERVALO_BUSQUEDA;
            }
        }
    }

    private Vector3 ElegirPuntoAleatorioEnZona()
    {
        BoxCollider[] cols = zonaCache.GetComponentsInChildren<BoxCollider>();
        if (cols == null || cols.Length == 0) return puntoGuardia;

        BoxCollider c = cols[Random.Range(0, cols.Length)];
        Bounds b = c.bounds;

        for (int i = 0; i < 8; i++)
        {
            Vector3 candidato = new Vector3(
                Random.Range(b.min.x, b.max.x),
                puntoGuardia.y,
                Random.Range(b.min.z, b.max.z));

            if (NavMesh.SamplePosition(candidato, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                return hit.position;
        }
        return puntoGuardia;
    }

    private void EnviarInformDone()
    {
        if (gestorActual == null || comms == null) return;

        MensajeFIPA informDone = new MensajeFIPA(
            "InformDone",
            comms,
            $"(inform-done (action {gameObject.name} " +
            $"(ir-a (coord {puntoGuardia.x:F1} {puntoGuardia.y:F1} {puntoGuardia.z:F1}))))",
            convIdActual,
            "fipa-contract-net");

        comms.Enviar(gestorActual, informDone);
    }

    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        enPosicion     = false;
        buscandoEnZona = false;
    }
}