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
        if (cfp.contenidoObjeto is not ContenidoCFP contenidoCFP)
        {
            comms.EnviarNotUnderstood(cfp);
            return;
        }

        if (gestorCN != null && gestorCN.HaySubastasActivas)
        {
            int cmp = string.Compare(gameObject.name, cfp.sender.gameObject.name,
                                     StringComparison.Ordinal);
            if (cmp < 0) return;
            gestorCN.AbortarSubastas();
        }

        if (!string.IsNullOrEmpty(conversacionBloqueActiva))
        {
            EnviarRefuse(cfp, "(agente-ya-asignado)");
            return;
        }

        if (modelo.ladronALaVista)
        {
            EnviarRefuse(cfp, "(agente-ocupado persiguiendo)");
            return;
        }

        Vector3 punto = contenidoCFP.puntoSalida;
        float   coste = CalcularCosteNavMesh(punto);

        if (coste >= float.MaxValue)
        {
            EnviarRefuse(cfp, "(punto-inalcanzable)");
            return;
        }

        ContenidoPropose oferta = new ContenidoPropose
        {
            puntoDestino = punto,
            costeNavMesh = coste
        };

        MensajeFIPA propose = new MensajeFIPA(
            "propose", comms,
            $"(ir-a (coord {punto.x:F1} {punto.y:F1} {punto.z:F1})) " +
            $"(coste-navegacion {coste:F1})",
            cfp.conversationId,
            cfp.protocol);
        propose.contenidoObjeto = oferta;
        propose.inReplyTo       = cfp.replyWith;
        propose.replyWith       = $"propose-{gameObject.name}-{Time.frameCount}";

        comms.Enviar(cfp.sender, propose);
    }

    // ── AcceptProposal ───────────────────────────────────────────────────────

    public void ProcesarAcceptProposal(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is not ContenidoTareaAsignada tarea)
        {
            comms.EnviarNotUnderstood(msj);
            return;
        }

        conversacionBloqueActiva = msj.conversationId;

        Debug.Log($"<color=cyan>[FIPA]</color> {gameObject.name}: " +
                  $"tarea [{tarea.tipoTarea}] aceptada → {tarea.puntoDestino} " +
                  $"[conv:{msj.conversationId}]");

        if (tarea.tipoTarea == TipoTarea.Investigar)
        {
            // Configurar Investigar y notificar a CapaReactiva → OnTareaInvestigar
            investigar.SetPuntoRuido(tarea.puntoDestino);
            capaCom?.NotificarTareaAsignada(tarea.puntoDestino, tarea.zonaNombre, TipoTarea.Investigar);
        }
        else
        {
            // Configurar BloquearSalida y notificar a CapaReactiva → OnTareaBloquear
            bloquearSalida.SetPunto(tarea.puntoDestino, msj.sender, msj.conversationId, tarea.zonaNombre);
            capaCom?.NotificarTareaAsignada(tarea.puntoDestino, tarea.zonaNombre, TipoTarea.Bloquear);
        }

        // Confirmar con Agree en ambos casos
        MensajeFIPA agree = new MensajeFIPA(
            "agree", comms,
            $"(ir-a (coord {tarea.puntoDestino.x:F1} {tarea.puntoDestino.y:F1} {tarea.puntoDestino.z:F1}))",
            msj.conversationId,
            msj.protocol);
        agree.inReplyTo = msj.replyWith;
        comms.Enviar(msj.sender, agree);
    }

    // ── Inform ───────────────────────────────────────────────────────────────

    public void ProcesarInform(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is ContenidoInformPosicion info)
        {
            modelo.RegistrarVerLadron(info.posicion, info.llevaElCuadro);
            modelo.RegistrarPerderLadron();
        }
    }

    public void ProcesarInformCuadroRobado(MensajeFIPA msj)
    {
        Debug.Log($"<color=orange>[CUADRO]</color> {gameObject.name}: " +
                  $"recibe aviso de cuadro robado de {msj.sender?.gameObject.name}");
        modelo.RegistrarFaltaCuadro();
    }

    // ── QueryIf ──────────────────────────────────────────────────────────────

    public void ProcesarQueryIf(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is not ContenidoQueryIf consulta) return;

        float distancia = Vector3.Distance(transform.position, consulta.posicionRuido);

        if (distancia <= UMBRAL_IDENTIFICACION_RUIDO)
        {
            ContenidoQueryIfRespuesta respuesta = new ContenidoQueryIfRespuesta
            {
                posicionReal = transform.position
            };

            MensajeFIPA confirm = new MensajeFIPA(
                "query-if-confirm", comms,
                $"(agente {gameObject.name}) " +
                $"(posicion {transform.position.x:F1} {transform.position.y:F1} {transform.position.z:F1})",
                msj.conversationId,
                "fipa-query");
            confirm.contenidoObjeto = respuesta;
            confirm.inReplyTo       = msj.replyWith;
            comms.Enviar(msj.sender, confirm);

            Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                      $"confirmo ser el origen del ruido (dist {distancia:F1}m)");
        }
    }

    public void ProcesarQueryIfConfirm(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is not ContenidoQueryIfRespuesta) return;

        Debug.Log($"<color=green>[QUERY-IF]</color> {gameObject.name}: " +
                  $"{msj.sender?.gameObject.name} confirma que el ruido era suyo. " +
                  $"[conv:{msj.conversationId}]");

        capaCom?.NotificarRuidoEraCompañero(msj.conversationId);
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    public void ProcesarCancel(MensajeFIPA cancel)
    {
        // El cancel puede referirse a un bloqueo o a una investigación CN.
        // Comprobamos ambos para saber si nos afecta.
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
            "(cancel-confirmado)",
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

    private void EnviarRefuse(MensajeFIPA cfp, string razon)
    {
        MensajeFIPA refuse = new MensajeFIPA(
            "refuse", comms, razon,
            cfp.conversationId, cfp.protocol);
        refuse.inReplyTo = cfp.replyWith;
        comms.Enviar(cfp.sender, refuse);
    }
}