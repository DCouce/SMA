using System;
using UnityEngine;
using UnityEngine.AI;

public class ProcesarComunicacion : MonoBehaviour
{
    private Modelado          modelo;
    private Control           control;
    private NavMeshAgent      navAgent;
    private BloquearSalida    bloquearSalida;
    private Investigar        investigar;
    private GestorContractNet gestorCN;
    private Mensajeria        comms;
    private CapaComunicacion  capaCom;

    private string conversacionBloqueActiva;

    private const float UMBRAL_IDENTIFICACION_RUIDO = 3.5f;

    void Awake()
    {
        modelo         = GetComponent<Modelado>();
        control        = GetComponent<Control>();
        navAgent       = GetComponent<NavMeshAgent>();
        bloquearSalida = GetComponent<BloquearSalida>();
        investigar     = GetComponent<Investigar>();
        gestorCN       = GetComponent<GestorContractNet>();
        comms          = GetComponent<Mensajeria>();
        capaCom        = GetComponent<CapaComunicacion>();
    }

    // ── CFP ─────────────────────────────────────────────────────────────────

    public void ProcesarCFP(MensajeFIPA cfp)
    {
        // Parsear tipo de tarea y punto del content
        var (tipoTarea, punto, zonaNombre) = MensajeFIPA.ParseCFP(cfp.content);

        if (gestorCN != null && gestorCN.HaySubastasActivas)
        {
            int cmp = string.Compare(gameObject.name, cfp.sender.gameObject.name,
                                     StringComparison.Ordinal);
            if (cmp < 0) return;
            gestorCN.AbortarSubastas();
        }

        if (!string.IsNullOrEmpty(conversacionBloqueActiva))
        {
            EnviarRefuse(cfp, "agente-ya-asignado");
            return;
        }

        if (modelo.ladronALaVista)
        {
            EnviarRefuse(cfp, "agente-ocupado");
            return;
        }

        float coste = CalcularCosteNavMesh(punto);
        if (coste >= float.MaxValue)
        {
            EnviarRefuse(cfp, "punto-inalcanzable");
            return;
        }

        MensajeFIPA propose = new MensajeFIPA(
            "propose", comms,
            MensajeFIPA.ContentPropose(punto, coste),
            cfp.conversationId,
            cfp.protocol);
        propose.inReplyTo = cfp.replyWith;
        propose.replyWith = $"propose-{gameObject.name}-{Time.frameCount}";

        comms.Enviar(cfp.sender, propose);
    }

    // ── AcceptProposal ───────────────────────────────────────────────────────

    public void ProcesarAcceptProposal(MensajeFIPA msj)
    {
        var (tipoTarea, punto, zonaNombre) = MensajeFIPA.ParseCFP(msj.content);

        conversacionBloqueActiva = msj.conversationId;

        Debug.Log($"<color=cyan>[FIPA]</color> {gameObject.name}: " +
                  $"tarea [{tipoTarea}] aceptada → {punto} [conv:{msj.conversationId}]");

        if (tipoTarea == TipoTarea.Investigar)
        {
            investigar.SetPuntoRuido(punto);
            capaCom?.NotificarTareaAsignada(punto, zonaNombre, TipoTarea.Investigar);
        }
        else
        {
            bloquearSalida.SetPunto(punto, msj.sender, msj.conversationId, zonaNombre);
            capaCom?.NotificarTareaAsignada(punto, zonaNombre, TipoTarea.Bloquear);
        }

        MensajeFIPA agree = new MensajeFIPA(
            "agree", comms,
            MensajeFIPA.ContentVec3(punto),
            msj.conversationId,
            msj.protocol);
        agree.inReplyTo = msj.replyWith;
        comms.Enviar(msj.sender, agree);
    }

    // ── Inform ───────────────────────────────────────────────────────────────

    public void ProcesarInform(MensajeFIPA msj)
    {
        // ontology == "posicion-ladron"  →  content: "x//y//z//llevaElCuadro"
        var (posicion, llevaElCuadro) = MensajeFIPA.ParseInformPosicion(msj.content);
        modelo.RegistrarVerLadron(posicion, llevaElCuadro);
        modelo.RegistrarPerderLadron();
    }

    public void ProcesarInformCuadroRobado(MensajeFIPA msj)
    {
        // ontology == "robo"  →  content: "x//y//z" (posición base, no usada aquí)
        Debug.Log($"<color=orange>[CUADRO]</color> {gameObject.name}: " +
                  $"recibe aviso de cuadro robado de {msj.sender?.gameObject.name}");
        modelo.RegistrarFaltaCuadro();
    }

    // ── QueryIf ──────────────────────────────────────────────────────────────

    public void ProcesarQueryIf(MensajeFIPA msj)
    {
        // content: "x//y//z"
        Vector3 posicionRuido = MensajeFIPA.ParseVec3(msj.content);
        float distancia = Vector3.Distance(transform.position, posicionRuido);

        if (distancia <= UMBRAL_IDENTIFICACION_RUIDO)
        {
            MensajeFIPA confirm = new MensajeFIPA(
                "query-if-confirm", comms,
                MensajeFIPA.ContentQueryIfConfirm(gameObject.name, transform.position),
                msj.conversationId,
                "fipa-query");
            confirm.inReplyTo = msj.replyWith;
            comms.Enviar(msj.sender, confirm);

            Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                      $"confirmo ser el origen del ruido (dist {distancia:F1}m)");
        }
    }

    public void ProcesarQueryIfConfirm(MensajeFIPA msj)
    {
        // content: "agente//x//y//z"
        var (agente, _) = MensajeFIPA.ParseQueryIfConfirm(msj.content);

        Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                  $"{agente} confirma que el ruido era suyo. [conv:{msj.conversationId}]");

        capaCom?.NotificarRuidoEraCompañero(msj.conversationId);
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    public void ProcesarCancel(MensajeFIPA cancel)
    {
        bool afectaBloqueo    = bloquearSalida != null &&
                                bloquearSalida.ConversacionActual() == cancel.conversationId;
        bool afectaInvestigar = conversacionBloqueActiva == cancel.conversationId;

        if (!afectaBloqueo && !afectaInvestigar) return;

        Debug.Log($"<color=yellow>[CANCEL]</color> {gameObject.name} abandona tarea CN " +
                  $"conv:{cancel.conversationId}");

        conversacionBloqueActiva = null;
        capaCom?.NotificarTareaCancelada();

        MensajeFIPA informDone = new MensajeFIPA(
            "inform-done", comms,
            "cancel-confirmado",
            cancel.conversationId,
            "fipa-cancel-meta-protocol");
        informDone.inReplyTo = cancel.replyWith;
        comms.Enviar(cancel.sender, informDone);
    }

    // ── Utils ─────────────────────────────────────────────────────────────────

    public float CalcularCosteNavMesh(Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        if (navAgent.CalculatePath(destino, path))
        {
            float d = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
                d += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            return d;
        }
        return float.MaxValue;
    }

    public void LiberarBloqueoActivo() => conversacionBloqueActiva = null;

    public void EnviarRefuse(MensajeFIPA cfp, string motivo)
    {
        MensajeFIPA refuse = new MensajeFIPA(
            "refuse", comms,
            motivo,
            cfp.conversationId,
            cfp.protocol);
        refuse.inReplyTo = cfp.replyWith;
        comms.Enviar(cfp.sender, refuse);
    }
}
