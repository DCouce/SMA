using UnityEngine;
using System.Collections.Generic;

// Agente coordinador aéreo (rol exclusivo de GESTOR en Contract Net).
//
// Al detectar al ladrón en una zona nueva lanza una cadena CN secuencial con DOS tipos de tarea:
//   [0] Investigar – el guardia más cercano entra a la zona a buscar al ladrón.
//   [1..N] Bloquear – un guardia distinto cubre cada punto de entrada/salida.
//
// La secuencialidad del CN y el filtro agentesAsignados garantizan que el guardia
// que investiga el interior NO pueda también bloquear una salida en la misma ronda.

public class DronComunicacion : MonoBehaviour
{
    [Header("Periodicidad")]
    [Tooltip("Segundos entre cada comprobación de zona.")]
    public float intervaloChequeo = 10f;

    private DronVision        vision;
    private GestorContractNet gestor;
    private Mensajeria        comms;

    private Zona  ultimaZonaContratada;
    private float proximoChequeo;

    // Red ocupada: inferido por observación de CFPs ajenos
    private bool  redOcupada        = false;
    private float tRedOcupadaExpira = 0f;
    private const float TIMEOUT_RED = 25f;

    void Awake()
    {
        vision = GetComponent<DronVision>();
        gestor = GetComponent<GestorContractNet>();
        comms  = GetComponent<Mensajeria>();
    }

    void OnEnable()
    {
        if (comms != null)
            comms.OnCFPRecibido += MarcarRedOcupada;
    }

    void OnDisable()
    {
        if (comms != null)
            comms.OnCFPRecibido -= MarcarRedOcupada;
    }

    void Update()
    {
        if (Time.time < proximoChequeo) return;
        proximoChequeo = Time.time + intervaloChequeo;

        if (vision.LadronVisible)
            ComprobarCambioDeZona(vision.PosicionLadron, vision.LadronRobo);
    }

    private void MarcarRedOcupada(Mensajeria emisor)
    {
        redOcupada        = true;
        tRedOcupadaExpira = Time.time + TIMEOUT_RED;
    }

    private void ComprobarCambioDeZona(Vector3 pos, bool llevaElCuadro)
    {
        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(pos);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaContratada) return;
        if (redOcupada && Time.time < tRedOcupadaExpira) return;

        redOcupada           = false;
        ultimaZonaContratada = zonaActual;

        Debug.Log($"<color=cyan>[DRON]</color> Ladrón en nueva zona: {zonaActual.nombreZona}. " +
                  $"Lanzando CN: 1 investigar + {zonaActual.puntosEntrada.Length} bloquear.");

        gestor.CancelarTodosLosContratos("dron-zona-cambiada");

        List<GestorContractNet.EntradaTarea> tareas = new List<GestorContractNet.EntradaTarea>();
        tareas.Add(new GestorContractNet.EntradaTarea
        {
            punto     = pos,
            tipoTarea = TipoTarea.Investigar
        });
        foreach (Transform punto in zonaActual.puntosEntrada)
        {
            tareas.Add(new GestorContractNet.EntradaTarea
            {
                punto     = punto.position,
                tipoTarea = TipoTarea.Bloquear
            });
        }

        gestor.IniciarContractNetsConTareas(tareas, zonaActual.nombreZona);
        DifundirPosicion(pos, llevaElCuadro, zonaActual.nombreZona);
    }

    private void DifundirPosicion(Vector3 pos, bool llevaElCuadro, string zonaNombre)
    {
        if (comms == null) return;

        MensajeFIPA inform = new MensajeFIPA(
            "inform", comms,
            MensajeFIPA.ContentInformPosicion(pos, llevaElCuadro),
            MensajeFIPA.GenerarConversationId(),
            "fipa-inform");
        inform.ontology = "posicion-ladron";

        comms.Difundir(inform);
    }
}
