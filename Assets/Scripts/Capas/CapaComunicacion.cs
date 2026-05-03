using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CapaComunicacion : MonoBehaviour
{
    // ── Eventos para CapaReactiva ────────────────────────────────────────────
    public event System.Action<Vector3, string> OnTareaBloquear;
    public event System.Action<Vector3, string> OnTareaInvestigar;
    public event System.Action OnTareaCancelada;

    [Header("QueryIf - Identificación de ruido")]
    public float timeoutQueryIf = 0.5f;
    public float distanciaMinimaRuido = 2f;

    private SensorVision sensor;
    private Mensajeria comms;
    private GestorContractNet gestor;
    private Modelado modelo;

    private Zona ultimaZonaCFP;
    private string convIdInformActual;

    private bool queryIfActivo = false;
    private bool queryIfConfirmado = false;
    private string queryIfConvId = null;
    private Vector3 posicionUltimoQueryIf = Vector3.positiveInfinity;

    // Red ocupada: inferido por observación de CFPs ajenos
    private bool redOcupada = false;
    private float tRedOcupadaExpira = 0f;
    private const float TIMEOUT_RED = 25f;
    private float tPerdidaLadron = -1f;
    private const float TIMEOUT_PERDER_LADRON = 5f;

    void Awake()
    {
        sensor = GetComponent<SensorVision>();
        comms = GetComponent<Mensajeria>();
        gestor = GetComponent<GestorContractNet>();
        modelo = GetComponent<Modelado>();
    }

    void Update()
    {
        if (tPerdidaLadron < 0f) return;
        if (Time.time - tPerdidaLadron < TIMEOUT_PERDER_LADRON) return;

        // Han pasado 5 segundos sin ver al ladrón entonces cancelar contratos activos
        tPerdidaLadron = -1f;
        ultimaZonaCFP  = null;

        if (gestor != null)
            gestor.CancelarTodosLosContratos("ladron-perdido-timeout");
    }
    void OnEnable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto += AlVerLadron;
            sensor.OnLadronPerdido += AlPerderLadron;
            sensor.OnCuadroRobadoDetectado += AlDetectarCuadroRobado;
        }
        if (modelo != null)
            modelo.OnRuidoPercibido += AlEscucharRuido;
        if (comms != null)
            comms.OnCFPRecibido += MarcarRedOcupada;
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto -= AlVerLadron;
            sensor.OnLadronPerdido -= AlPerderLadron;
            sensor.OnCuadroRobadoDetectado -= AlDetectarCuadroRobado;
        }
        if (modelo != null)
            modelo.OnRuidoPercibido -= AlEscucharRuido;
        if (comms != null)
            comms.OnCFPRecibido -= MarcarRedOcupada;
    }

    private void MarcarRedOcupada(Mensajeria emisor)
    {
        // Si el CFP viene del dron, no bloqueamos la red para los guardias
        if (emisor != null && emisor.GetComponent<DronComunicacion>() != null) return;

        redOcupada = true;
        tRedOcupadaExpira = Time.time + TIMEOUT_RED;
    }

    private void AlVerLadron(Vector3 pos, bool llevaElCuadro)
    {
        tPerdidaLadron = -1f;             // volvemos a verlo, cancelamos el countdown
        ComprobarCambioDeZona(pos, llevaElCuadro);
    }

    private void AlPerderLadron()
    {
        convIdInformActual = null;
        tPerdidaLadron = Time.time;   // empezamos a contar
    }

    private void AlDetectarCuadroRobado()
    {
        if (modelo.sabeRobado) return;

        Debug.Log($"<color=orange>[CUADRO]</color> {gameObject.name}: " +
                  $"detecta que el cuadro no está. Difundiendo InformCuadroRobado.");

        Vector3 posBase = sensor.posicionBaseCuadro != null
            ? sensor.posicionBaseCuadro.position
            : Vector3.zero;

        MensajeFIPA inform = new MensajeFIPA(
            "inform", comms,
            MensajeFIPA.ContentVec3(posBase),
            MensajeFIPA.GenerarConversationId(),
            "fipa-inform");
        inform.ontology = "robo";

        comms.Difundir(inform);
        modelo.RegistrarFaltaCuadro();
    }

    private void AlEscucharRuido(Vector3 posicionRuido)
    {
        if (queryIfActivo) return;

        float distancia = Vector3.Distance(posicionRuido, posicionUltimoQueryIf);
        if (distancia < distanciaMinimaRuido) return;

        posicionUltimoQueryIf = posicionRuido;
        StartCoroutine(CicloQueryIf(posicionRuido));
    }

    private IEnumerator CicloQueryIf(Vector3 posicionRuido)
    {
        queryIfActivo = true;
        queryIfConfirmado = false;
        queryIfConvId = MensajeFIPA.GenerarConversationId();

        MensajeFIPA queryIf = new MensajeFIPA(
            "query-if", comms,
            MensajeFIPA.ContentVec3(posicionRuido),
            queryIfConvId,
            "fipa-query");

        Debug.Log($"<color=yellow>[QUERY-IF]</color> {gameObject.name}: " +
                  $"ruido en {posicionRuido}. Lanzando QueryIf [conv:{queryIfConvId}]");

        comms.Difundir(queryIf);
        yield return new WaitForSeconds(timeoutQueryIf);

        if (queryIfConfirmado)
        {
            Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                      $"ruido identificado como compañero. Ignorando. [conv:{queryIfConvId}]");
        }
        else
        {
            Debug.Log($"<color=red>[QUERY-IF]</color> {gameObject.name}: " +
                      $"sin respuesta → ruido sospechoso. [conv:{queryIfConvId}]");
            ComprobarCambioDeZona(posicionRuido, modelo.sabeRobado);
        }

        queryIfActivo = false;
        queryIfConvId = null;
    }

    private void ComprobarCambioDeZona(Vector3 pos, bool llevaElCuadro)
    {
        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(pos);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaCFP) return;
        if (redOcupada && Time.time < tRedOcupadaExpira) return;
        if (GestorContractNet.ZonaGestionadaActualmente == zonaActual &&
            Time.time < GestorContractNet.tZonaGestionadaExpira) return;
        redOcupada    = false;
        ultimaZonaCFP = zonaActual;
        EnviarInformCambioZona(pos, llevaElCuadro);

        if (gestor != null)
        {
            gestor.CancelarTodosLosContratos("cambio-de-zona");
            gestor.IniciarContractNetsSecuenciales(
                new List<Transform>(zonaActual.puntosEntrada),
                zonaActual.nombreZona);
        }
    }
    private void EnviarInformCambioZona(Vector3 pos, bool llevaElCuadro)
    {
        if (comms == null) return;

        convIdInformActual = MensajeFIPA.GenerarConversationId();

        MensajeFIPA inform = new MensajeFIPA(
            "inform", comms,
            MensajeFIPA.ContentInformPosicion(pos, llevaElCuadro),
            convIdInformActual,
            "fipa-inform");
        inform.ontology = "posicion-ladron";

        comms.Difundir(inform);
    }

    // Llamadas desde ProcesarComunicacion
    // Activa el comportamiento correcto en CapaReactiva según el tipo de tarea CN ganada.
    public void NotificarTareaAsignada(Vector3 punto, string zona, string tipoTarea)
    {
        if (tipoTarea == TipoTarea.Investigar)
            OnTareaInvestigar?.Invoke(punto, zona);
        else
            OnTareaBloquear?.Invoke(punto, zona);
    }

    public void NotificarTareaCancelada()
        => OnTareaCancelada?.Invoke();

    public void NotificarRuidoEraCompañero(string conversationId)
    {
        if (conversationId != queryIfConvId) return;
        queryIfConfirmado = true;
        Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                  $"confirmación recibida [conv:{conversationId}]");
    }
}