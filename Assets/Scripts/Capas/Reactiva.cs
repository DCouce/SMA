using UnityEngine;
using System.Collections.Generic;

public class CapaReactiva : MonoBehaviour
{
    private SensorVision      sensor;
    private Control           control;
    private Perseguir         perseguir;
    private Capturar          capturar;
    private GestorContractNet gestor;
    private Comunicacion      comms;

    private float tiempoUltimoInform = -999f;
    private const float COOLDOWN_INFORM = 0.5f;
    private string convIdInformActual;

    // Recordamos la última zona para la que disparamos un CFP.
    // Solo lanzamos uno nuevo si el ladrón cambia de zona.
    private Zona ultimaZonaCFP;

    void Awake()
    {
        sensor    = GetComponent<SensorVision>();
        control   = GetComponent<Control>();
        perseguir = GetComponent<Perseguir>();
        capturar  = GetComponent<Capturar>();
        gestor    = GetComponent<GestorContractNet>();
        comms     = GetComponent<Comunicacion>();
    }

    void OnEnable()
    {
        sensor.OnLadronVisto   += ReaccionarAlInstante;
        sensor.OnLadronPerdido += PararEmergencia;
    }

    private void ReaccionarAlInstante(Vector3 pos, bool robado)
    {
        CompartirPosicionLadron(pos, robado);

        float distancia = Vector3.Distance(transform.position, pos);

        if (distancia < 0.2f)
        {
            control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, capturar);
            return;
        }

        perseguir.ActualizarObjetivo(pos);
        control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, perseguir);

        // Solo lanzamos un nuevo Contract Net si la zona del ladrón cambió.
        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(pos);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaCFP) return;

        ultimaZonaCFP = zonaActual;
        gestor.IniciarContractNet(
            new List<Transform>(zonaActual.puntosEntrada),
            zonaActual.nombreZona);
    }

    private void CompartirPosicionLadron(Vector3 pos, bool llevaElCuadro)
    {
        if (comms == null) return;
        if (Time.time - tiempoUltimoInform < COOLDOWN_INFORM) return;
        tiempoUltimoInform = Time.time;

        if (convIdInformActual == null)
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
            $"(lleva-cuadro {llevaElCuadro.ToString().ToLower()}))",
            convIdInformActual,
            "fipa-request");
        inform.contenidoObjeto = contenido;

        comms.Difundir(inform);
    }

    private void PararEmergencia()
    {
        convIdInformActual = null;
        ultimaZonaCFP      = null;   // al perderlo de vista, volvemos a empezar
        control.RetirarPropuesta(Control.PRIORIDAD_REACTIVA);
    }

    void OnDisable()
    {
        if (sensor)
        {
            sensor.OnLadronVisto   -= ReaccionarAlInstante;
            sensor.OnLadronPerdido -= PararEmergencia;
        }
    }
}