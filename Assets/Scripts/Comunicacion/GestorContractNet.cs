using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Implementa el rol de GESTOR en el protocolo FIPA Contract Net.
//
// Los Contract Net se ejecutan de forma SECUENCIAL: se lanza el CFP
// del primer punto, se espera a recoger propuestas, se asigna al ganador,
// y SOLO ENTONCES se lanza el CFP del siguiente punto.
// Esto garantiza que el segundo CN ya sabe qué agentes están ocupados.
//
// Flujo por tarea:
//   1. Gestor envía CFP (una tarea, un punto) a todos los agentes.
//   2. Contratistas responden con Propose o Refuse.
//   3. Gestor evalúa propuestas y envía AcceptProposal al mejor
//      y RejectProposal al resto.
//   4. Se pasa al siguiente punto (si queda alguno).

public class GestorContractNet : MonoBehaviour
{
    [Tooltip("Tiempo de espera para recoger propuestas antes de evaluar (segundos).")]
    public float tiempoEsperaPropuestas = 0.3f;

    private Mensajeria comms;

    // Subasta en curso (solo una a la vez)
    private string convIdActual;
    private List<MensajeFIPA> propuestasActuales = new List<MensajeFIPA>();
    private bool subastaEnCurso = false;

    // Contratos ya asignados en esta ronda (para cancelar al cambiar de zona)
    private readonly List<ContratoActivo> contratosActivos = new List<ContratoActivo>();

    // Agentes que ya ganaron un punto en esta ronda (no pueden ganar otro)
    private readonly HashSet<Mensajeria> agentesAsignados = new HashSet<Mensajeria>();

    public bool HaySubastasActivas => subastaEnCurso || contratosActivos.Count > 0;

    private class ContratoActivo
    {
        public string conversationId;
        public Mensajeria contratista;
        public Vector3 punto;
    }

    void Awake()
    {
        comms = GetComponent<Mensajeria>();
    }


    // Inicia una cadena secuencial de Contract Nets: uno por cada punto.
    // Los puntos se procesan en orden, esperando a que cada uno termine
    // antes de lanzar el siguiente.
    public void IniciarContractNetsSecuenciales(List<Transform> puntos, string zonaNombre)
    {
        StartCoroutine(CadenaSecuencial(puntos, zonaNombre));
    }

    // Recibe un Propose de un contratista (llamado desde Comunicacion)
    public void RecibirPropuesta(MensajeFIPA propose)
    {
        if (!subastaEnCurso) return;
        if (propose.conversationId != convIdActual) return;

        propuestasActuales.Add(propose);
    }

    // Recibe un InformDone del contratista: tarea completada o cancelación confirmada
    public void RecibirInformDone(MensajeFIPA informDone)
    {
        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name}: " +
                  $"{informDone.sender?.gameObject.name} completó su tarea. " +
                  $"[conv:{informDone.conversationId}]");

        LiberarContrato(informDone.conversationId, informDone.sender);
    }

    private void LiberarContrato(string conversationId, Mensajeria contratista)
    {
        ContratoActivo contrato = contratosActivos.Find(
            c => c.conversationId == conversationId);
        if (contrato != null)
        {
            agentesAsignados.Remove(contrato.contratista);
            contratosActivos.Remove(contrato);
        }
    }

    // Aborta todo (desempate con otro gestor): cancela contratos activos y detiene la cadena
    public void AbortarSubastas()
    {
        CancelarTodosLosContratos("desempate");
        Debug.Log($"[{gameObject.name}] Subastas abortadas por desempate.");
    }

    // Cancela todos los contratos activos (cambio de zona, fin de juego)
    public void CancelarTodosLosContratos(string razon)
    {
        // Cancelar la cadena secuencial si está en curso
        StopAllCoroutines();
        subastaEnCurso = false;
        propuestasActuales.Clear();

        List<ContratoActivo> copia = new List<ContratoActivo>(contratosActivos);
        // Enviar Cancel a todos los contratistas con tarea asignada
        foreach (ContratoActivo contrato in copia)
        {
            if (contrato.contratista != null)
            {
                MensajeFIPA cancel = new MensajeFIPA(
                    "cancel",
                    comms,
                    $"(razon {razon})",
                    contrato.conversationId,
                    "fipa-cancel-meta-protocol");

                comms.Enviar(contrato.contratista, cancel);
            }
        }

        contratosActivos.Clear();
        agentesAsignados.Clear();
    }

    //  CADENA SECUENCIAL: un CN tras otro
    private IEnumerator CadenaSecuencial(List<Transform> puntos, string zonaNombre)
    {
        foreach (Transform punto in puntos)
        {
            yield return StartCoroutine(RondaContractNet(punto.position, zonaNombre));
        }
    }

    // ═══════════════════════════════════════════════════════
    //  COROUTINE: una ronda Contract Net para UN punto
    // ═══════════════════════════════════════════════════════

    private IEnumerator RondaContractNet(Vector3 punto, string zonaNombre)
    {
        subastaEnCurso = true;
        propuestasActuales.Clear();
        convIdActual = MensajeFIPA.GenerarConversationId();

        // PASO 1: enviar CFP
        ContenidoCFP contenidoCFP = new ContenidoCFP
        {
            puntoSalida = punto,
            zonaNombre  = zonaNombre
        };

        MensajeFIPA cfp = new MensajeFIPA(
            "cfp",
            comms,
            $"(bloquear-salida (coord {punto.x:F1} {punto.y:F1} {punto.z:F1}))",
            convIdActual,
            "fipa-contract-net");
        cfp.contenidoObjeto = contenidoCFP;
        cfp.replyWith       = $"cfp-{gameObject.name}-{convIdActual}";

        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name} lanza CFP " +
                  $"para punto {punto} [conv:{convIdActual}]");

        comms.Difundir(cfp);
        // PASO 2: esperar propuestas
        yield return new WaitForSeconds(tiempoEsperaPropuestas);

        // PASO 3: evaluar y asignar
        AsignarMejorOferta(punto, zonaNombre);

        subastaEnCurso = false;
    }

    //  EVALUACIÓN: elegir al mejor postor
    private void AsignarMejorOferta(Vector3 punto, string zonaNombre)
    {
        if (propuestasActuales.Count == 0)
        {
            Debug.LogWarning($"[CONTRACT NET] Sin propuestas para {punto}");
            return;
        }

        // Filtrar agentes que ya están asignados a otro punto en esta ronda
        List<MensajeFIPA> disponibles = propuestasActuales
            .Where(p => !agentesAsignados.Contains(p.sender))
            .ToList();

        if (disponibles.Count == 0)
        {
            Debug.LogWarning($"[CONTRACT NET] Todos los postores ya están asignados " +
                             $"para {punto}");
            return;
        }

        // Ordenar por coste NavMesh (menor = mejor)
        disponibles.Sort((a, b) =>
        {
            float ca = ((ContenidoPropose)a.contenidoObjeto).costeNavMesh;
            float cb = ((ContenidoPropose)b.contenidoObjeto).costeNavMesh;
            return ca.CompareTo(cb);
        });

        MensajeFIPA ganadora    = disponibles[0];
        ContenidoPropose oferta = (ContenidoPropose)ganadora.contenidoObjeto;

        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name} asigna " +
                  $"{punto} → {ganadora.sender?.gameObject.name} " +
                  $"(coste {oferta.costeNavMesh:F1}m) [conv:{convIdActual}]");

        // Marcar como asignado
        agentesAsignados.Add(ganadora.sender);
        contratosActivos.Add(new ContratoActivo
        {
            conversationId = convIdActual,
            contratista    = ganadora.sender,
            punto          = punto
        });

        // AcceptProposal al ganador
        ContenidoTareaAsignada tarea = new ContenidoTareaAsignada
        {
            puntoDestino = punto,
            zonaNombre   = zonaNombre
        };

        MensajeFIPA accept = new MensajeFIPA(
            "accept-proposal",
            comms,
            $"(ir-a (coord {punto.x:F1} {punto.y:F1} {punto.z:F1}))",
            convIdActual,
            "fipa-contract-net");
        accept.contenidoObjeto = tarea;
        accept.inReplyTo       = ganadora.replyWith;

        comms.Enviar(ganadora.sender, accept);

        // RejectProposal al resto
        foreach (MensajeFIPA propuesta in propuestasActuales)
        {
            if (propuesta == ganadora) continue;

            MensajeFIPA reject = new MensajeFIPA(
                "reject-proposal",
                comms,
                "(mejor-oferta-seleccionada)",
                convIdActual,
                "fipa-contract-net");
            reject.inReplyTo = propuesta.replyWith;

            comms.Enviar(propuesta.sender, reject);
        }
    }
}