using UnityEngine;
using System.Collections.Generic;

// Capa de Comunicación: funciona en PARALELO a Reactiva, Planificación y Modelado.
// Se activa mediante eventos de los sensores y gestiona toda la comunicación FIPA:
//   - Difunde Informs con la posición del ladrón (cooldown + distancia mínima).
//   - Lanza Contract Nets individuales (uno por tarea/punto de salida).
//   - Cancela contratos anteriores cuando la zona cambia.
//
// Emite eventos que la CapaReactiva consume para decidir acciones.

public class CapaComunicacion : MonoBehaviour
{
    // Eventos que la CapaReactiva escucha
    public event System.Action<Vector3, bool> OnLadronDetectado;
    public event System.Action OnLadronPerdido;
    public event System.Action<Vector3, string> OnTareaAsignada;  // punto + zona
    public event System.Action OnTareaCancelada;

    private SensorVision      sensor;
    private Mensajeria      comms;
    private GestorContractNet gestor;
    private Modelado          modelo;

    // Control de zona: se usa tanto para Contract Net como para Inform
    // El Inform solo se envía cuando se detecta una zona nueva (o la primera)
    private Zona ultimaZonaCFP;
    private string convIdInformActual;

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
            sensor.OnLadronVisto   += AlVerLadron;
            sensor.OnLadronPerdido += AlPerderLadron;
        }
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto   -= AlVerLadron;
            sensor.OnLadronPerdido -= AlPerderLadron;
        }
    }

    // ─── EVENTO: el sensor ve al ladrón ───────────────────────────

    private void AlVerLadron(Vector3 pos, bool llevaElCuadro)
    {
        // 1. Comprobar cambio de zona → lanzar Contract Nets + enviar Inform
        ComprobarCambioDeZona(pos, llevaElCuadro);

        // 2. Notificar a la capa reactiva
        OnLadronDetectado?.Invoke(pos, llevaElCuadro);
    }

    // ─── EVENTO: el sensor pierde al ladrón ──────────────────────

    private void AlPerderLadron()
    {
        convIdInformActual = null;
        ultimaZonaCFP      = null;

        OnLadronPerdido?.Invoke();
    }

    // ─── CAMBIO DE ZONA: Inform + Contract Net ────────────────────
    // El Inform se envía UNA sola vez por zona (al entrar en una nueva,
    // incluida la primera detección). Esto cumple la semántica FIPA:
    // solo informamos cuando hay información nueva que el receptor no tiene.

    private void ComprobarCambioDeZona(Vector3 pos, bool llevaElCuadro)
    {
        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(pos);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaCFP) return;  // misma zona, nada que hacer

        ultimaZonaCFP = zonaActual;

        // Enviar Inform: el ladrón está en una zona nueva
        EnviarInformCambioZona(pos, llevaElCuadro, zonaActual.nombreZona);

        // Cancelar contratos anteriores y lanzar nuevos Contract Nets secuenciales
        if (gestor != null)
        {
            gestor.CancelarTodosLosContratos("cambio-de-zona");

            // Se pasa la lista completa de puntos: el gestor los procesará
            // uno tras otro, esperando a que cada subasta se resuelva
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

    // ─── API: llamada desde ProcesarComunicacion cuando nos asignan tarea ─

    public void NotificarTareaAsignada(Vector3 punto, string zona)
    {
        OnTareaAsignada?.Invoke(punto, zona);
    }

    public void NotificarTareaCancelada()
    {
        OnTareaCancelada?.Invoke();
    }
}