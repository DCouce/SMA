using System;
using UnityEngine;
using UnityEngine.AI;

// Procesa los mensajes FIPA recibidos y ejecuta la lógica correspondiente.
// Separado de Comunicacion (transporte) y de CapaComunicacion (capa paralela).
//
// Responsabilidades:
//   - Procesar CFP: decidir si pujar, calcular coste, enviar Propose/Refuse
//   - Procesar AcceptProposal: activar BloquearSalida y notificar a CapaComunicacion
//   - Procesar Inform: actualizar el modelo de creencias
//   - Procesar Cancel: liberar tareas y notificar a CapaComunicacion

public class ProcesarComunicacion : MonoBehaviour
{
    private Modelado          modelo;
    private Control           control;
    private NavMeshAgent      navAgent;
    private BloquearSalida    bloquearSalida;
    private GestorContractNet gestorCN;
    private Mensajeria      comms;
    private CapaComunicacion  capaCom;

    // Conversación de bloqueo activa (para saber si un Cancel nos atañe)
    private string conversacionBloqueActiva;

    void Awake()
    {
        modelo         = GetComponent<Modelado>();
        control        = GetComponent<Control>();
        navAgent       = GetComponent<NavMeshAgent>();
        bloquearSalida = GetComponent<BloquearSalida>();
        gestorCN       = GetComponent<GestorContractNet>();
        comms          = GetComponent<Mensajeria>();
        capaCom        = GetComponent<CapaComunicacion>();
    }

    // ═══════════════════════════════════════════════════════
    //  PROCESAR CFP (rol contratista)
    // ═══════════════════════════════════════════════════════

    public void ProcesarCFP(MensajeFIPA cfp)
    {
        if (cfp.contenidoObjeto is not ContenidoCFP contenidoCFP)
        {
            comms.EnviarNotUnderstood(cfp);
            return;
        }

        // Desempate determinista: si tenemos nuestra propia subasta activa
        if (gestorCN != null && gestorCN.HaySubastasActivas)
        {
            int cmp = string.Compare(gameObject.name, cfp.sender.gameObject.name,
                                     StringComparison.Ordinal);
            if (cmp < 0)
            {
                // Ganamos el desempate: ignoramos este CFP
                return;
            }
            // Perdemos el desempate: abortamos nuestras subastas
            gestorCN.AbortarSubastas();
        }

        // Si ya estamos bloqueando una salida, rechazamos
        if (!string.IsNullOrEmpty(conversacionBloqueActiva))
        {
            MensajeFIPA refuse = new MensajeFIPA(
                "Refuse",
                comms,
                $"(refuse (action {gameObject.name} bloquear-salida) (agente-ya-asignado))",
                cfp.conversationId,
                cfp.protocol);
            refuse.inReplyTo = cfp.replyWith;
            comms.Enviar(cfp.sender, refuse);
            return;
        }

        // Si estamos persiguiendo, rechazamos
        if (modelo.ladronALaVista)
        {
            MensajeFIPA refuse = new MensajeFIPA(
                "Refuse",
                comms,
                $"(refuse (action {gameObject.name} bloquear-salida) (agente-ocupado persiguiendo))",
                cfp.conversationId,
                cfp.protocol);
            refuse.inReplyTo = cfp.replyWith;
            comms.Enviar(cfp.sender, refuse);
            return;
        }

        // Calcular coste para el punto y enviar Propose
        Vector3 punto = contenidoCFP.puntoSalida;
        float coste = CalcularCosteNavMesh(punto);
        if (coste >= float.MaxValue) return; // punto inalcanzable, no respondemos

        ContenidoPropose oferta = new ContenidoPropose
        {
            puntoDestino = punto,
            costeNavMesh = coste
        };

        MensajeFIPA propose = new MensajeFIPA(
            "Propose",
            comms,
            $"(propose (action {gameObject.name} (ir-a " +
            $"(coord {punto.x:F1} {punto.y:F1} {punto.z:F1}))) " +
            $"(= (coste-navegacion) {coste:F1}))",
            cfp.conversationId,
            cfp.protocol);
        propose.contenidoObjeto = oferta;
        propose.inReplyTo       = cfp.replyWith;
        propose.replyWith       = $"propose-{gameObject.name}-{Time.frameCount}";

        comms.Enviar(cfp.sender, propose);
    }

    // ═══════════════════════════════════════════════════════
    //  PROCESAR ACCEPT-PROPOSAL (rol contratista)
    // ═══════════════════════════════════════════════════════

    public void ProcesarAcceptProposal(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is not ContenidoTareaAsignada tarea)
        {
            comms.EnviarNotUnderstood(msj);
            return;
        }

        conversacionBloqueActiva = msj.conversationId;

        Debug.Log($"<color=cyan>[FIPA]</color> {gameObject.name}: " +
                  $"tarea aceptada → {tarea.puntoDestino} [conv:{msj.conversationId}]");

        // Configurar BloquearSalida con los datos del gestor
        bloquearSalida.SetPunto(tarea.puntoDestino, msj.sender, msj.conversationId, tarea.zonaNombre);

        // Notificar a la CapaComunicacion → que notifique a la CapaReactiva
        capaCom?.NotificarTareaAsignada(tarea.puntoDestino, tarea.zonaNombre);

        // Confirmar con Agree
        MensajeFIPA agree = new MensajeFIPA(
            "Agree",
            comms,
            $"(agree (action {gameObject.name} (ir-a " +
            $"(coord {tarea.puntoDestino.x:F1} {tarea.puntoDestino.y:F1} {tarea.puntoDestino.z:F1}))) true)",
            msj.conversationId,
            msj.protocol);
        agree.inReplyTo = msj.replyWith;
        comms.Enviar(msj.sender, agree);
    }

    // ═══════════════════════════════════════════════════════
    //  PROCESAR INFORM (posición del ladrón compartida)
    // ═══════════════════════════════════════════════════════

    public void ProcesarInform(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is ContenidoInformPosicion info)
        {
            modelo.RegistrarVerLadron(info.posicion, info.llevaElCuadro);
            modelo.RegistrarPerderLadron();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  PROCESAR CANCEL (el gestor cancela nuestro contrato)
    // ═══════════════════════════════════════════════════════

    public void ProcesarCancel(MensajeFIPA cancel)
    {
        if (bloquearSalida == null) return;
        if (bloquearSalida.ConversacionActual() != cancel.conversationId) return;

        Debug.Log($"<color=yellow>[CANCEL]</color> {gameObject.name} abandona bloqueo " +
                  $"conv:{cancel.conversationId}");

        conversacionBloqueActiva = null;

        // Notificar a la CapaComunicacion → que notifique a la CapaReactiva
        capaCom?.NotificarTareaCancelada();

        // Responder con InformDone
        MensajeFIPA informDone = new MensajeFIPA(
            "InformDone",
            comms,
            "(inform-done (cancel-confirmado))",
            cancel.conversationId,
            "fipa-contract-net");
        informDone.inReplyTo = cancel.replyWith;
        comms.Enviar(cancel.sender, informDone);
    }

    // ═══════════════════════════════════════════════════════
    //  UTILIDADES
    // ═══════════════════════════════════════════════════════

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

    // Permite liberar la conversación de bloqueo desde fuera (ej: fin de juego)
    public void LiberarBloqueoActivo()
    {
        conversacionBloqueActiva = null;
    }
}