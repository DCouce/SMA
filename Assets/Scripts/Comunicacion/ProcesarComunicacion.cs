using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using System.ComponentModel;
using System.Security.AccessControl;
using System.Threading.Tasks;

public class ProcesarComunicacion : MonoBehaviour
{
    private Modelado modelo;
    private Control control;
    private NavMeshAgent navAgent;
    private BloquearSalida bloquearSalida;
    private GestorContractNet gestorCN;
    private HistorialConversaciones historial;
    private string conversacionBloqueActiva;
    private Comunicacion comms;

    void Awake()
    {
        modelo         = GetComponent<Modelado>();
        control        = GetComponent<Control>();
        navAgent       = GetComponent<NavMeshAgent>();
        bloquearSalida = GetComponent<BloquearSalida>();
        gestorCN       = GetComponent<GestorContractNet>();
        historial      = GetComponent<HistorialConversaciones>();
        comms          = GetComponent<Comunicacion>();


    }

    // MANEJADORES DE PERFORMATIVAS

    // Recibe un CFP: decide si puede pujar y envía Propose o Refuse.
    // No puja si ya está persiguiendo al ladrón.
    public void ProcesarCFP(MensajeFIPA cfp)
    {
        if (cfp.contenidoObjeto is not ContenidoCFP contenidoCFP)
        {
            comms.EnviarNotUnderstood(cfp);
            return;
        }

        // Si ya hay una tarea de bloqueo de salida en ejecución, rechazamos
        // Se consulta el historial para saberlo
        if (historial != null && historial.EstaBloqueandoSalida())
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

        // Si estamos persiguiendo, nos negamos (Refuse)
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

        // Calculamos el coste para cada punto y enviamos un Propose por punto
        foreach (Transform punto in contenidoCFP.puntosASalida)
        {
            float coste = CalcularCosteNavMesh(punto.position);
            if (coste >= float.MaxValue) continue; // punto inalcanzable

            ContenidoPropose oferta = new ContenidoPropose
            {
                puntoDestino = punto.position,
                costeNavMesh = coste
            };

            MensajeFIPA propose = new MensajeFIPA(
                "Propose",
                comms,
                $"(propose (action {gameObject.name} (ir-a (coord {punto.position.x:F1} {punto.position.y:F1} {punto.position.z:F1}))) (= (coste-navegacion) {coste:F1}))",
                cfp.conversationId,
                cfp.protocol);
            propose.contenidoObjeto = oferta;
            propose.inReplyTo       = cfp.replyWith;
            propose.replyWith       = $"propose-{gameObject.name}-{Time.frameCount}";

            comms.Enviar(cfp.sender, propose);
        }
    }

    // Recibe AcceptProposal: activa BloquearSalida hacia el punto asignado
    public void ProcesarAcceptProposal(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is not ContenidoTareaAsignada tarea)
        {
            comms.EnviarNotUnderstood(msj);
            return;
        }
        conversacionBloqueActiva = msj.conversationId;
        Debug.Log($"<color=cyan>[FIPA]</color> {gameObject.name}: tarea aceptada → {tarea.puntoDestino}");

        bloquearSalida.SetPunto(tarea.puntoDestino, msj.sender, msj.conversationId);
        control.RecibirPropuesta(Control.PRIORIDAD_SUBASTA, bloquearSalida);

        // Confirmamos con Agree
        MensajeFIPA agree = new MensajeFIPA(
            "Agree",
            comms,
            $"(agree (action {gameObject.name} (ir-a (coord {tarea.puntoDestino.x:F1} {tarea.puntoDestino.y:F1} {tarea.puntoDestino.z:F1}))) true)",
            msj.conversationId,
            msj.protocol);
        agree.inReplyTo = msj.replyWith;
        comms.Enviar(msj.sender, agree);
    }

    // Procesa un Inform genérico según su contenido semántico.
    // Caso actual: posición del ladrón compartida entre guardias.
    public async Task ProcesarInform(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is ContenidoInformPosicion info)
        {
            // Actualizamos creencias con la información recibida
            modelo.RegistrarVerLadron(info.posicion, info.llevaElCuadro); 
            modelo.RegistrarPerderLadron();                   
        }
    }
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
}