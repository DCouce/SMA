using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Capa de Comunicación: funciona en PARALELO a Reactiva, Planificación y Modelado.
//
// Emite dos eventos distintos para que CapaReactiva active el comportamiento correcto:
//   - OnTareaBloquear   → el guardia debe ir a bloquear un punto de salida (BloquearSalida)
//   - OnTareaInvestigar → el guardia debe entrar a la zona a buscar al ladrón (Investigar)
//   - OnTareaCancelada  → el gestor canceló el contrato activo

public class CapaComunicacion : MonoBehaviour
{
    // ── Eventos para CapaReactiva ────────────────────────────────────────────
    public event System.Action<Vector3, string> OnTareaBloquear;
    public event System.Action<Vector3, string> OnTareaInvestigar;
    public event System.Action                  OnTareaCancelada;

    [Header("QueryIf – Identificación de ruido")]
    public float timeoutQueryIf      = 0.5f;
    public float distanciaMinimaRuido = 2f;

    private SensorVision      sensor;
    private Mensajeria        comms;
    private GestorContractNet gestor;
    private Modelado          modelo;

    private Zona   ultimaZonaCFP;
    private string convIdInformActual;

    private bool   queryIfActivo        = false;
    private bool   queryIfConfirmado    = false;
    private string queryIfConvId        = null;
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

    private void AlVerLadron(Vector3 pos, bool llevaElCuadro)
        => ComprobarCambioDeZona(pos, llevaElCuadro);

    private void AlPerderLadron()
    {
        convIdInformActual = null;
        ultimaZonaCFP      = null;
    }

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
            "inform", comms,
            $"(cuadro-robado (posicion {sensor.posicionBaseCuadro?.position.x:F1} " +
            $"{sensor.posicionBaseCuadro?.position.y:F1} {sensor.posicionBaseCuadro?.position.z:F1}))",
            MensajeFIPA.GenerarConversationId(),
            "fipa-inform");
        inform.contenidoObjeto = contenido;

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
        queryIfActivo     = true;
        queryIfConfirmado = false;
        queryIfConvId     = MensajeFIPA.GenerarConversationId();

        ContenidoQueryIf contenido = new ContenidoQueryIf { posicionRuido = posicionRuido };

        MensajeFIPA queryIf = new MensajeFIPA(
            "query-if", comms,
            $"(agente-en-posicion (coord {posicionRuido.x:F1} {posicionRuido.y:F1} {posicionRuido.z:F1}))",
            queryIfConvId,
            "fipa-query");
        queryIf.contenidoObjeto = contenido;

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
            modelo.RegistrarRuido(posicionRuido);
        }

        queryIfActivo = false;
        queryIfConvId = null;
    }

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
            "inform", comms,
            $"(ubicacion ladron (coord {pos.x:F1} {pos.y:F1} {pos.z:F1})) " +
            $"(zona {zonaNombre}) " +
            $"(lleva-cuadro {llevaElCuadro.ToString().ToLower()})",
            convIdInformActual,
            "fipa-inform");
        inform.contenidoObjeto = contenido;

        comms.Difundir(inform);
    }

    // ── Llamadas desde ProcesarComunicacion ─────────────────────────────────

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