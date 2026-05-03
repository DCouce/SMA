using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GestorContractNet : MonoBehaviour
{
    [Tooltip("Tiempo de espera para recoger propuestas antes de evaluar (segundos).")]
    public float tiempoEsperaPropuestas = 0.3f;

    private Mensajeria comms;

    private string            convIdActual;
    private List<MensajeFIPA> propuestasActuales = new List<MensajeFIPA>();
    private bool              subastaEnCurso     = false;

    private readonly List<ContratoActivo> contratosActivos = new List<ContratoActivo>();
    private readonly HashSet<Mensajeria>  agentesAsignados = new HashSet<Mensajeria>();

    public bool HaySubastasActivas => subastaEnCurso || contratosActivos.Count > 0;

    private class ContratoActivo
    {
        public string     conversationId;
        public Mensajeria contratista;
        public Vector3    punto;
    }

    public struct EntradaTarea
    {
        public Vector3 punto;
        public string  tipoTarea;
    }

    void Awake() => comms = GetComponent<Mensajeria>();

    // ── API pública ──────────────────────────────────────────────────────────

    public void IniciarContractNetsSecuenciales(List<Transform> puntos, string zonaNombre)
    {
        List<EntradaTarea> tareas = puntos
            .Select(t => new EntradaTarea { punto = t.position, tipoTarea = TipoTarea.Bloquear })
            .ToList();
        StartCoroutine(CadenaSecuencial(tareas, zonaNombre));
    }

    public void IniciarContractNetsConTareas(List<EntradaTarea> tareas, string zonaNombre)
        => StartCoroutine(CadenaSecuencial(tareas, zonaNombre));

    // ── Recepción de mensajes ────────────────────────────────────────────────

    public void RecibirPropuesta(MensajeFIPA propose)
    {
        if (!subastaEnCurso) return;
        if (propose.conversationId != convIdActual) return;
        propuestasActuales.Add(propose);
    }

    public void RecibirInformDone(MensajeFIPA informDone)
    {
        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name}: " +
                  $"{informDone.sender?.gameObject.name} completó su tarea. " +
                  $"[conv:{informDone.conversationId}]");
        LiberarContrato(informDone.conversationId);
    }

    private void LiberarContrato(string conversationId)
    {
        ContratoActivo contrato = contratosActivos.Find(c => c.conversationId == conversationId);
        if (contrato != null)
        {
            agentesAsignados.Remove(contrato.contratista);
            contratosActivos.Remove(contrato);
        }
    }

    public void AbortarSubastas()
    {
        CancelarTodosLosContratos("desempate");
        Debug.Log($"[{gameObject.name}] Subastas abortadas por desempate.");
    }

    public void CancelarTodosLosContratos(string razon)
    {
        StopAllCoroutines();
        subastaEnCurso = false;
        propuestasActuales.Clear();

        foreach (ContratoActivo contrato in new List<ContratoActivo>(contratosActivos))
        {
            if (contrato.contratista == null) continue;
            comms.Enviar(contrato.contratista, new MensajeFIPA(
                "cancel", comms,
                razon,
                contrato.conversationId,
                "fipa-cancel-meta-protocol"));
        }

        contratosActivos.Clear();
        agentesAsignados.Clear();
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator CadenaSecuencial(List<EntradaTarea> tareas, string zonaNombre)
    {
        foreach (EntradaTarea tarea in tareas)
            yield return StartCoroutine(RondaContractNet(tarea.punto, tarea.tipoTarea, zonaNombre));
    }

    private IEnumerator RondaContractNet(Vector3 punto, string tipoTarea, string zonaNombre)
    {
        subastaEnCurso = true;
        propuestasActuales.Clear();
        convIdActual = MensajeFIPA.GenerarConversationId();

        MensajeFIPA cfp = new MensajeFIPA(
            "cfp", comms,
            MensajeFIPA.ContentCFP(tipoTarea, punto, zonaNombre),
            convIdActual,
            "fipa-contract-net");
        cfp.replyWith = $"cfp-{gameObject.name}-{convIdActual}";

        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name} lanza CFP " +
                  $"[{tipoTarea}] para {punto} [conv:{convIdActual}]");

        comms.Difundir(cfp);
        yield return new WaitForSeconds(tiempoEsperaPropuestas);

        AsignarMejorOferta(punto, tipoTarea, zonaNombre);
        subastaEnCurso = false;
    }

    private void AsignarMejorOferta(Vector3 punto, string tipoTarea, string zonaNombre)
    {
        if (propuestasActuales.Count == 0)
        {
            Debug.LogWarning($"[CONTRACT NET] Sin propuestas para {punto} [{tipoTarea}]");
            return;
        }

        List<MensajeFIPA> disponibles = propuestasActuales
            .Where(p => !agentesAsignados.Contains(p.sender))
            .ToList();

        if (disponibles.Count == 0)
        {
            Debug.LogWarning($"[CONTRACT NET] Todos los postores ya asignados para {punto}");
            return;
        }

        // Ordenar por coste (campo 3 del content del propose: x//y//z//cost)
        disponibles.Sort((a, b) =>
        {
            float ca = MensajeFIPA.ParsePropose(a.content).cost;
            float cb = MensajeFIPA.ParsePropose(b.content).cost;
            return ca.CompareTo(cb);
        });

        MensajeFIPA ganadora = disponibles[0];
        float coste = MensajeFIPA.ParsePropose(ganadora.content).cost;

        Debug.Log($"<color=cyan>[CONTRACT NET]</color> {gameObject.name} asigna " +
                  $"[{tipoTarea}] {punto} → {ganadora.sender?.gameObject.name} " +
                  $"(coste {coste:F1}m) [conv:{convIdActual}]");

        agentesAsignados.Add(ganadora.sender);
        contratosActivos.Add(new ContratoActivo
        {
            conversationId = convIdActual,
            contratista    = ganadora.sender,
            punto          = punto
        });

        MensajeFIPA accept = new MensajeFIPA(
            "accept-proposal", comms,
            MensajeFIPA.ContentCFP(tipoTarea, punto, zonaNombre),
            convIdActual,
            "fipa-contract-net");
        accept.inReplyTo = ganadora.replyWith;
        comms.Enviar(ganadora.sender, accept);

        foreach (MensajeFIPA propuesta in propuestasActuales)
        {
            if (propuesta == ganadora) continue;
            MensajeFIPA reject = new MensajeFIPA(
                "reject-proposal", comms,
                "mejor-oferta-seleccionada",
                convIdActual,
                "fipa-contract-net");
            reject.inReplyTo = propuesta.replyWith;
            comms.Enviar(propuesta.sender, reject);
        }
    }
}
