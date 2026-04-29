using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Capa de Comunicación: funciona en PARALELO a Reactiva, Planificación y Modelado.
// Se activa mediante eventos de los sensores y gestiona toda la comunicación FIPA:
//   - Difunde Informs con la posición del ladrón (cooldown + distancia mínima).
//   - Lanza Contract Nets individuales (uno por tarea/punto de salida).
//   - Cancela contratos anteriores cuando la zona cambia.
//   - Al escuchar un ruido, envía UN SOLO QueryIf y espera respuesta durante
//     timeoutQueryIf segundos. Si nadie responde, el ruido se registra como
//     sospechoso. Nuevos disparos del Oido durante ese tiempo se descartan
//     para evitar inundar la red con QueryIfs redundantes.
//   - Al detectar que el cuadro no está, difunde InformCuadroRobado para que
//     todos los compañeros actualicen su modelo de creencias.
//
// Emite eventos que la CapaReactiva consume para decidir acciones.

public class CapaComunicacion : MonoBehaviour
{
    // Eventos que la CapaReactiva escucha
    public event System.Action<Vector3, bool> OnLadronDetectado;
    public event System.Action OnLadronPerdido;
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

    // ─── EVENTO: el sensor ve al ladrón ──────────────────────────────────────

    private void AlVerLadron(Vector3 pos, bool llevaElCuadro)
    {
        ComprobarCambioDeZona(pos, llevaElCuadro);
        OnLadronDetectado?.Invoke(pos, llevaElCuadro);
    }

    // ─── EVENTO: el sensor pierde al ladrón ──────────────────────────────────

    private void AlPerderLadron()
    {
        convIdInformActual = null;
        ultimaZonaCFP      = null;
        OnLadronPerdido?.Invoke();
    }

    // ─── EVENTO: el sensor detecta que el cuadro no está ─────────────────────

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

    // ─── EVENTO: el Modelado relanza un ruido percibido ──────────────────────
    //
    // LÓGICA DE FILTRADO (en orden):
    //   1. Si ya hay un QueryIf activo → descartar (el primero cubre este ruido).
    //   2. Si el nuevo ruido está a menos de distanciaMinimaRuido del anterior
    //      QueryIf → descartar (mismo evento sonoro, distinto fotograma).
    //   3. En otro caso → lanzar un nuevo QueryIf y bloquear nuevos disparos
    //      hasta que se resuelva.

    private void AlEscucharRuido(Vector3 posicionRuido)
    {
        // Filtro 1: ya hay un QueryIf en vuelo
        if (queryIfActivo)
        {
            Debug.Log($"[{gameObject.name}] Ruido descartado: QueryIf ya en curso.");
            return;
        }

        // Filtro 2: mismo foco sonoro que el QueryIf anterior (pisadas continuas)
        float distancia = Vector3.Distance(posicionRuido, posicionUltimoQueryIf);
        if (distancia < distanciaMinimaRuido)
        {
            Debug.Log($"[{gameObject.name}] Ruido descartado: demasiado cerca del anterior " +
                      $"QueryIf ({distancia:F1}m < {distanciaMinimaRuido}m).");
            return;
        }

        // Lanzar QueryIf
        StartCoroutine(QueryIfYActuar(posicionRuido));
    }

    // ─── COROUTINE: un solo QueryIf por foco sonoro ───────────────────────────
    //
    // 1. Marca queryIfActivo = true (bloquea nuevos disparos).
    // 2. Difunde el QueryIf con la posición del ruido.
    // 3. Espera timeoutQueryIf segundos.
    // 4. Si queryIfConfirmado → alguien respondió: ignorar.
    //    Si no                → nadie respondió: registrar como ruido sospechoso.
    // 5. Resetea el estado para permitir el siguiente QueryIf.

    private IEnumerator QueryIfYActuar(Vector3 posicionRuido)
    {
        // Bloquear nuevos QueryIfs
        queryIfActivo         = true;
        queryIfConfirmado     = false;
        queryIfConvId         = MensajeFIPA.GenerarConversationId();
        posicionUltimoQueryIf = posicionRuido;

        // Construir y difundir QueryIf
        ContenidoQueryIf consulta = new ContenidoQueryIf { posicionRuido = posicionRuido };

        MensajeFIPA queryIf = new MensajeFIPA(
            "QueryIf",
            comms,
            $"(query-if (= (origen-ruido ?agente) " +
            $"(coord {posicionRuido.x:F1} {posicionRuido.y:F1} {posicionRuido.z:F1})))",
            queryIfConvId,
            "fipa-query");
        queryIf.contenidoObjeto = consulta;
        queryIf.replyWith       = $"queryif-{gameObject.name}-{queryIfConvId}";

        comms.Difundir(queryIf);

        Debug.Log($"<color=yellow>[QUERY-IF]</color> {gameObject.name}: " +
                  $"QueryIf enviado → ruido en {posicionRuido} " +
                  $"(espera {timeoutQueryIf}s) [conv:{queryIfConvId}]");

        // Esperar respuesta
        yield return new WaitForSeconds(timeoutQueryIf);

        // Evaluar resultado
        if (queryIfConfirmado)
        {
            Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                      $"ruido confirmado como compañero → ignorado. [conv:{queryIfConvId}]");
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

    // ─── CAMBIO DE ZONA: Inform + Contract Net ────────────────────────────────

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

    // ─── API pública ──────────────────────────────────────────────────────────

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