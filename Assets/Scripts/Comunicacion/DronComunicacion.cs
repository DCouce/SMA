using UnityEngine;
using System.Collections.Generic;

// Agente coordinador aéreo (rol exclusivo de GESTOR en Contract Net).
//
// Comportamiento:
//   - Cada `intervaloChequeo` segundos consulta DronVision directamente.
//   - Si el ladrón es visible Y está en una zona distinta a la del último CN lanzado:
//       1. Cancela los contratos anteriores.
//       2. Lanza un CN secuencial para bloquear las salidas de la nueva zona.
//       3. Difunde un Inform con la posición para actualizar a todos los guardias.
//   - Si el ladrón no cambió de zona, no hace nada (evita spam de CNs).
//
// Componentes necesarios en el mismo GameObject:
//   DroeVision, GestorContractNet, Mensajeria.
// No necesita CapaReactiva, CapaPlanificacion ni ProcesarComunicacion.

public class DronComunicacion : MonoBehaviour
{
    [Header("Periodicidad")]
    [Tooltip("Segundos entre cada comprobación de zona.")]
    public float intervaloChequeo = 10f;

    private DronVision       vision;
    private GestorContractNet gestor;
    private Mensajeria        comms;

    private Zona  ultimaZonaContratada;
    private float proximoChequeo;

    void Awake()
    {
        vision = GetComponent<DronVision>();
        gestor = GetComponent<GestorContractNet>();
        comms  = GetComponent<Mensajeria>();
    }

    void Update()
    {
        if (Time.time < proximoChequeo) return;
        proximoChequeo = Time.time + intervaloChequeo;

        if (!vision.LadronVisible) return;

        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(vision.PosicionLadron);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaContratada) return;

        ultimaZonaContratada = zonaActual;

        Debug.Log($"<color=cyan>[DRON]</color> Ladrón en nueva zona: {zonaActual.nombreZona}. " +
                  $"Lanzando Contract Net.");

        gestor.CancelarTodosLosContratos("dron-zona-cambiada");
        gestor.IniciarContractNetsSecuenciales(
            new List<Transform>(zonaActual.puntosEntrada),
            zonaActual.nombreZona);

        DifundirPosicion(zonaActual.nombreZona);
    }

    private void DifundirPosicion(string zonaNombre)
    {
        if (comms == null) return;

        Vector3 pos = vision.PosicionLadron;

        ContenidoInformPosicion contenido = new ContenidoInformPosicion
        {
            posicion      = pos,
            llevaElCuadro = vision.LadronRobo
        };

        MensajeFIPA inform = new MensajeFIPA(
            "Inform",
            comms,
            $"(posicion-ladron (= (ubicacion ladron) " +
            $"(coord {pos.x:F1} {pos.y:F1} {pos.z:F1})) " +
            $"(zona {zonaNombre}) " +
            $"(lleva-cuadro {vision.LadronRobo.ToString().ToLower()}))",
            MensajeFIPA.GenerarConversationId(),
            "fipa-inform");
        inform.contenidoObjeto = contenido;

        comms.Difundir(inform);
    }
}
