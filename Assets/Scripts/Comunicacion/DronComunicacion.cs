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
//   DronVision, GestorContractNet, Mensajeria.
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

        if (vision.LadronVisible)
            ComprobarCambioDeZona(vision.PosicionLadron, vision.LadronRobo);
    }

    // CAMBIO DE ZONA: Inform + Contract Net
    private void ComprobarCambioDeZona(Vector3 pos, bool llevaElCuadro)
    {
        Zona zonaActual = GestorZonas.Instance?.ObtenerZona(pos);
        if (zonaActual == null || zonaActual.puntosEntrada.Length == 0) return;
        if (zonaActual == ultimaZonaContratada) return;

        ultimaZonaContratada = zonaActual;

        Debug.Log($"<color=cyan>[DRON]</color> Ladrón en nueva zona: {zonaActual.nombreZona}. " +
                  $"Lanzando Contract Net.");

        gestor.CancelarTodosLosContratos("dron-zona-cambiada");
        gestor.IniciarContractNetsSecuenciales(
            new List<Transform>(zonaActual.puntosEntrada),
            zonaActual.nombreZona);

        DifundirPosicion(pos, llevaElCuadro, zonaActual.nombreZona);
    }

    private void DifundirPosicion(Vector3 pos, bool llevaElCuadro, string zonaNombre)
    {
        if (comms == null) return;

        ContenidoInformPosicion contenido = new ContenidoInformPosicion
        {
            posicion      = pos,
            llevaElCuadro = llevaElCuadro
        };

        MensajeFIPA inform = new MensajeFIPA(
            "inform",
            comms,
            $"(ubicacion ladron (coord {pos.x:F1} {pos.y:F1} {pos.z:F1})) " +
            $"(zona {zonaNombre}) " +
            $"(lleva-cuadro {llevaElCuadro.ToString().ToLower()})",
            MensajeFIPA.GenerarConversationId(),
            "fipa-inform");
        inform.contenidoObjeto = contenido;

        comms.Difundir(inform);
    }
}
