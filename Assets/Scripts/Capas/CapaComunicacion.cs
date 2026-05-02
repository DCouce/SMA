using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Capa de Comunicación: funciona en PARALELO a Reactiva, Planificación y Modelado.
// Es la frontera entre el agente y la red FIPA. Gestiona:
//   - Difunde Informs con la posición del ladrón al cambiar de zona.
//   - Lanza Contract Nets secuenciales (uno por punto de salida).
//   - Cancela contratos anteriores cuando la zona cambia.
//   - Al escuchar un ruido, envía UN SOLO QueryIf y espera respuesta durante
//     timeoutQueryIf segundos. Si nadie responde, registra el ruido como sospechoso.
//   - Al detectar que el cuadro no está, difunde InformCuadroRobado.
//
// Emite eventos que la CapaReactiva consume para acciones derivadas de la red:
//   - OnTareaAsignada  → el guardia debe bloquear un punto (Contract Net ganado)
//   - OnTareaCancelada → el gestor canceló el contrato

public class CapaComunicacion : MonoBehaviour
{
    // Eventos que la CapaReactiva escucha (solo tareas de red, no percepciones locales)
    public event System.Action<Vector3, string> OnTareaAsignada;
    public event System.Action OnTareaCancelada;

    [Header("QueryIf – Identificación de ruido")]
    [Tooltip("Segundos que se espera respuesta al QueryIf antes de investigar el ruido.")]
    public float timeoutQueryIf = 0.5f;

    [Tooltip("Distancia mínima entre dos posiciones de ruido para considerar que son " +
             "sucesos distintos y lanzar un nuevo QueryIf. Evita duplicados del mismo paso.")]
    public float distanciaMinimaRuido = 2f;

    private SensorVision      sensor;
    private Mensajeria        comms;
    private GestorContractNet gestor;
    private Modelado          modelo;

    // Control de zona
    private Zona   ultimaZonaCFP;
    private string convIdInformActual;

    // ── Estado del QueryIf en curso ───────────────────────────────────────────
    // Solo puede haber un QueryIf activo a la vez por agente.
    // Mientras queryIfActivo es true, cualquier nuevo ruido se descarta.
    private bool   queryIfActivo        = false;
    private bool   queryIfConfirmado    = false; // true si alguien respondió
    private string queryIfConvId        = null;  // conv-id del QueryIf en curso

    // Posición del último ruido que lanzó un QueryIf, para el filtro de distancia.
    private Vector3 posicionUltimoQueryIf = Vector3.positiveInfinity;

    void Awake()
    {
        sensor = GetComponent<SensorVision>();
        comms  = GetComponent<Mensajeria>();
        gestor = GetComponent<GestorContractNet>();
        modelo = GetComponent<Modelado>();
    }

    void OnEnable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto           += AlVerLadron;
            sensor.OnLadronPerdido         += AlPerderLadron;
            sensor.OnCuadroRobadoDetectado += AlDetectarCuadroRobado;
        }

        if (modelo != null)
            modelo.OnRuidoPercibido += AlEscucharRuido;
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto           -= AlVerLadron;
            sensor.OnLadronPerdido         -= AlPerderLadron;
            sensor.OnCuadroRobadoDetectado -= AlDetectarCuadroRobado;
        }

        if (modelo != null)
            modelo.OnRuidoPercibido -= AlEscucharRuido;
    }

    // EVENTO: el sensor ve al ladrón
    // Solo gestiona la comunicación con la red (Inform + Contract Net).
    // La CapaReactiva reacciona directamente al sensor para su propio comportamiento.

    private void AlVerLadron(Vector3 pos, bool llevaElCuadro)
    {
        ComprobarCambioDeZona(pos, llevaElCuadro);
    }

    // EVENTO: el sensor pierde al ladrón
    private void AlPerderLadron()
    {
        convIdInformActual = null;
        ultimaZonaCFP      = null;
    }

    // EVENTO: el sensor detecta que el cuadro no está
    private void AlDetectarCuadroRobado()
    {
        if (modelo.sabeRobado) return;

        Debug.Log($"<color=orange>[CUADRO]</color> {gameObject.name}: " +
                  $"detecta que el cuadro no está. Difundiendo InformCuadroRobado.");

        ContenidoInformCuadroRobado contenido = new ContenidoInformCuadroRobado
        {
            posicionBase = sensor.posicionBaseCuadro != null
                ? sensor.posicionBaseCuadro.position
                : Vector3.zero
        };

        MensajeFIPA inform = new MensajeFIPA(
            "InformCuadroRobado",
            comms,
            "(inform (cuadro-robado true))",
            MensajeFIPA.GenerarConversationId(),
            "fipa-inform");
        inform.contenidoObjeto = contenido;

        comms.Difundir(inform);
        modelo.RegistrarFaltaCuadro();
    }

    //  EVENTO: el Modelado relanza un ruido percibido
    // LÓGICA DE FILTRADO (en orden):
    //   1. Si ya hay un QueryIf activo: descartar (el primero cubre este ruido).
    //   2. Si el ruido está muy cerca del último QueryIf: descartar (mismo suceso).
    //   3. En caso contrario: lanzar QueryIf y esperar timeout.

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
        queryIfActivo     = false;
        queryIfConfirmado = false;
        queryIfConvId     = MensajeFIPA.GenerarConversationId();
        queryIfActivo     = true;

        // Construir y difundir el QueryIf
        ContenidoQueryIf contenido = new ContenidoQueryIf { posicionRuido = posicionRuido };

        MensajeFIPA queryIf = new MensajeFIPA(
            "QueryIf",
            comms,
            $"(query-if (agente-en-posicion " +
            $"(coord {posicionRuido.x:F1} {posicionRuido.y:F1} {posicionRuido.z:F1})))",
            queryIfConvId,
            "fipa-query");
        queryIf.contenidoObjeto = contenido;

        Debug.Log($"<color=yellow>[QUERY-IF]</color> {gameObject.name}: " +
                  $"ruido en {posicionRuido}. Lanzando QueryIf [conv:{queryIfConvId}]");

        comms.Difundir(queryIf);

        // Esperar timeout
        yield return new WaitForSeconds(timeoutQueryIf);

        if (queryIfConfirmado)
        {
            Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                      $"ruido identificado como compañero. Ignorando. " +
                      $"[conv:{queryIfConvId}]");
        }
        else
        {
            Debug.Log($"<color=red>[QUERY-IF]</color> {gameObject.name}: " +
                      $"sin respuesta en {timeoutQueryIf}s → ruido sospechoso. " +
                      $"[conv:{queryIfConvId}]");
            modelo.RegistrarRuido(posicionRuido);
        }

        // Liberar estado para el siguiente ciclo
        queryIfActivo = false;
        queryIfConvId = null;
    }

    // CAMBIO DE ZONA: Inform + Contract Net
    private void ComprobarCambioDeZona(Vector3 pos, bool llevaElCuadro)
    {
        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(pos);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaCFP) return;

        ultimaZonaCFP = zonaActual;
        EnviarInformCambioZona(pos, llevaElCuadro, zonaActual.nombreZona);

        if (gestor != null)
        {
            gestor.CancelarTodosLosContratos("cambio-de-zona");
            gestor.IniciarContractNetsSecuenciales(
                new List<Transform>(zonaActual.puntosEntrada),
                zonaActual.nombreZona);
        }
    }

    private void EnviarInformCambioZona(Vector3 pos, bool llevaElCuadro, string zonaNombre)
    {
        if (comms == null) return;

        convIdInformActual = MensajeFIPA.GenerarConversationId();

        ContenidoInformPosicion contenido = new ContenidoInformPosicion
        {
            posicion      = pos,
            llevaElCuadro = llevaElCuadro
        };

        MensajeFIPA inform = new MensajeFIPA(
            "Inform",
            comms,
            $"(posicion-ladron (= (ubicacion ladron) " +
            $"(coord {pos.x:F1} {pos.y:F1} {pos.z:F1})) " +
            $"(zona {zonaNombre}) " +
            $"(lleva-cuadro {llevaElCuadro.ToString().ToLower()}))",
            convIdInformActual,
            "fipa-request");
        inform.contenidoObjeto = contenido;

        comms.Difundir(inform);
    }


    public void NotificarTareaAsignada(Vector3 punto, string zona)
        => OnTareaAsignada?.Invoke(punto, zona);

    public void NotificarTareaCancelada()
        => OnTareaCancelada?.Invoke();

    // Llamada desde ProcesarComunicacion cuando llega un QueryIfConfirm.
    // Solo actúa si el conv-id coincide con el QueryIf que tenemos en vuelo.
    public void NotificarRuidoEraCompañero(string conversationId)
    {
        if (conversationId != queryIfConvId) return;

        queryIfConfirmado = true;

        Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                  $"confirmación recibida [conv:{conversationId}]");
    }
}