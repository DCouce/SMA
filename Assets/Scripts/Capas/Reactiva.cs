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
        }
        else
        {
            perseguir.ActualizarObjetivo(pos);
            control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, perseguir);

            // Pedir a los demás guardias que bloqueen las salidas de la zona
            Zona zona = GestorZonas.Instance?.ObtenerZona(pos);

            if (zona != null && zona.puntosEntrada.Length > 0)
                gestor.IniciarContractNet(new List<Transform>(zona.puntosEntrada));
        }
    }

    // Difunde la posición conocida del ladrón a los demás guardias
    // mediante un Inform FIPA-ACL. Esto actualiza las creencias de los
    // agentes que no tienen línea de visión directa.
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
            FIPAPerformativa.Inform,
            comms,
            $"(posicion-ladron (= (ubicacion ladron) " +
            $"(coord {pos.x:F1} {pos.y:F1} {pos.z:F1})) " +
            $"(lleva-cuadro {llevaElCuadro.ToString().ToLower()}))",
            convIdInformActual,
            "fipa-request");
        inform.contenidoObjeto = contenido;
        inform.ontology        = "museo-seguridad";

        comms.Difundir(inform);
    }

    private void PararEmergencia()
    {
        convIdInformActual = null;
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