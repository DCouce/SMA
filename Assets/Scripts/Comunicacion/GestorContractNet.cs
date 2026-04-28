using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Implementa el rol de GESTOR en el protocolo FIPA Contract Net.
//
// Flujo del protocolo:
//   1. Gestor envía CFP a todos los agentes.
//   2. Contratistas responden con Propose o Refuse.
//   3. Gestor evalúa propuestas y envía AcceptProposal al mejor
//      y RejectProposal al resto.
//   4. Contratista confirma con Agree y finaliza con InformDone o Failure.
//
// El gestor es el guardia que avista al ladrón (CapaReactiva lo activa).
public class GestorContractNet : MonoBehaviour
{
    [Tooltip("Tiempo de espera para recoger propuestas antes de evaluar (segundos).")]
    public float tiempoEsperaPropuestas = 0.3f;

    private Comunicacion comms;
    private HistorialConversaciones historial;

    // Estado de la subasta activa
    private bool subastaActiva = false;
    private string conversationIdActual;

    // Propuestas recibidas en la ronda actual: punto → lista de propuestas
    private readonly Dictionary<Vector3, List<MensajeFIPA>> propuestasPorPunto
        = new Dictionary<Vector3, List<MensajeFIPA>>();

    // Puntos a cubrir en la ronda actual
    private List<Vector3> puntosPendientes = new List<Vector3>();

    void Awake()
    {
        comms     = GetComponent<Comunicacion>();
        historial = GetComponent<HistorialConversaciones>();
    }

    // API PÚBLICA (llamada desde CapaReactiva)

    // Inicia una nueva ronda Contract Net para cubrir los puntos de salida dados.
    // Si ya hay una subasta activa, no lanza otra.
    public void IniciarContractNet(List<Transform> puntos)
    {
        if (subastaActiva) return;
        StartCoroutine(RondaContractNet(puntos));
    }

    // Recibe un Propose de un contratista (llamado desde Comunicacion)
    public void RecibirPropuesta(MensajeFIPA propose)
    {
        if (!subastaActiva) return;
        if (propose.conversationId != conversationIdActual) return;

        ContenidoPropose oferta = (ContenidoPropose)propose.contenidoObjeto;

        if (!propuestasPorPunto.ContainsKey(oferta.puntoDestino))
            propuestasPorPunto[oferta.puntoDestino] = new List<MensajeFIPA>();

        propuestasPorPunto[oferta.puntoDestino].Add(propose);
    }

    // Recibe un InformDone del contratista: tarea completada
    public void RecibirInformDone(MensajeFIPA informDone)
    {
        // El historial ya actualiza el estado automáticamente al registrar InformDone
        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name}: " +
                  $"{informDone.sender?.gameObject.name} completó su tarea. " +
                  $"[conv:{informDone.conversationId}]");
    }

    // COROUTINE PRINCIPAL DEL PROTOCOLO

    private IEnumerator RondaContractNet(List<Transform> puntos)
    {
        subastaActiva = true;
        propuestasPorPunto.Clear();
        puntosPendientes     = puntos.Select(p => p.position).ToList();
        conversationIdActual = MensajeFIPA.GenerarConversationId();

        // PASO 1: enviar CFP
        string replyWithCFP = $"cfp-{gameObject.name}-{Time.frameCount}";

        ContenidoCFP contenidoCFP = new ContenidoCFP { puntosASalida = puntos.ToArray() };

        // Construimos el content en FIPA-SL
        string puntosStr = string.Join(" ",
            puntos.Select(p =>
                $"(coord {p.position.x:F1} {p.position.y:F1} {p.position.z:F1})"));

        MensajeFIPA cfp = new MensajeFIPA(
            "CallForProposal",
            comms,
            $"(action ?agente (bloquear-salida (set {puntosStr})))",
            conversationIdActual,
            "fipa-contract-net");
        cfp.contenidoObjeto = contenidoCFP;
        cfp.replyWith       = replyWithCFP;

        comms.Difundir(cfp);

        // PASO 2: esperar propuestas
        yield return new WaitForSeconds(tiempoEsperaPropuestas);

        // PASO 3: evaluar y asignar
        foreach (Vector3 punto in puntosPendientes)
            AsignarMejorOferta(punto);

        subastaActiva = false;
    }

    // Para el punto dado, elige la propuesta de menor coste,
    // envía AcceptProposal al ganador y RejectProposal al resto.
    private void AsignarMejorOferta(Vector3 punto)
    {
        if (!propuestasPorPunto.TryGetValue(punto, out List<MensajeFIPA> propuestas)
            || propuestas.Count == 0)
        {
            Debug.LogWarning($"[CONTRACT NET] Sin propuestas para {punto}");
            return;
        }

        // Ordenamos por coste NavMesh (menor = mejor)
        propuestas.Sort((a, b) =>
        {
            float ca = ((ContenidoPropose)a.contenidoObjeto).costeNavMesh;
            float cb = ((ContenidoPropose)b.contenidoObjeto).costeNavMesh;
            return ca.CompareTo(cb);
        });

        MensajeFIPA ganadora     = propuestas[0];
        ContenidoPropose oferta  = (ContenidoPropose)ganadora.contenidoObjeto;

        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name} asigna " +
                  $"{punto} → {ganadora.sender?.gameObject.name} " +
                  $"(coste {oferta.costeNavMesh:F1}m)");

        // AcceptProposal al ganador
        ContenidoTareaAsignada tarea = new ContenidoTareaAsignada { puntoDestino = punto };

        MensajeFIPA accept = new MensajeFIPA(
            "AcceptProposal",
            comms,
            $"(accept-proposal (action {ganadora.sender?.gameObject.name} " +
            $"(ir-a (coord {punto.x:F1} {punto.y:F1} {punto.z:F1}))) true)",
            conversationIdActual,
            "fipa-contract-net");
        accept.contenidoObjeto = tarea;
        accept.inReplyTo       = ganadora.replyWith;

        comms.Enviar(ganadora.sender, accept);

        // RejectProposal al resto
        for (int i = 1; i < propuestas.Count; i++)
        {
            MensajeFIPA perdedora = propuestas[i];

            MensajeFIPA reject = new MensajeFIPA(
                "RejectProposal",
                comms,
                $"(reject-proposal (action {perdedora.sender?.gameObject.name} bloquear-salida) " +
                $"(mejor-oferta-seleccionada))",
                conversationIdActual,
                "fipa-contract-net");
            reject.inReplyTo = perdedora.replyWith;

            comms.Enviar(perdedora.sender, reject);
        }
    }
}