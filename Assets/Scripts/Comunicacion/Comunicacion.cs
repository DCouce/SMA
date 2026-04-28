using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

// Capa de transporte FIPA-ACL del agente guardia.
// Gestiona el envío y recepción de mensajes conforme al estándar FIPA,
// y los delega al componente apropiado según la performativa y el protocolo.

public class Comunicacion : MonoBehaviour
{
    // Red de agentes registrados
    private static readonly List<Comunicacion> red = new List<Comunicacion>();

    public static IReadOnlyList<Comunicacion> Red => red;

    // Referencias internas
    private Modelado modelo;
    private Control control;
    private NavMeshAgent navAgent;
    private BloquearSalida bloquearSalida;
    private GestorContractNet gestorCN;
    private HistorialConversaciones historial;
    private string conversacionBloqueActiva;

    void Awake()
    {
        modelo         = GetComponent<Modelado>();
        control        = GetComponent<Control>();
        navAgent       = GetComponent<NavMeshAgent>();
        bloquearSalida = GetComponent<BloquearSalida>();
        gestorCN       = GetComponent<GestorContractNet>();
        historial      = GetComponent<HistorialConversaciones>();

        red.Add(this);
    }

    void OnDestroy() => red.Remove(this);

    // ENVÍO

    // Envía un mensaje FIPA directamente a un receptor específico.
    public void Enviar(Comunicacion receptor, MensajeFIPA msj)
    {
        msj.sender   = this;
        msj.receiver = new[] { receptor };

        historial?.RegistrarEnviado(msj);
        Debug.Log($"<color=green>[FIPA →]</color> {msj}");
        receptor.Recibir(msj);
    }

    // Difunde un mensaje a todos los agentes de la red excepto a sí mismo
    public void Difundir(MensajeFIPA msj)
    {
        msj.sender = this;

        historial?.RegistrarEnviado(msj);
        Debug.Log($"<color=lime>[FIPA ↦]</color> {msj}");
        foreach (Comunicacion agente in red)
        {
            if (agente != this && agente != null)
                agente.Recibir(msj);
        }
    }

    // RECEPCIÓN

    // Punto de entrada de todos los mensajes FIPA recibidos.
    public void Recibir(MensajeFIPA msj)
    {
        historial?.RegistrarRecibido(msj);

        Debug.Log($"<color=cyan>[FIPA ←]</color> {gameObject.name} recibe {msj.performativa} " +
                  $"de {msj.sender?.gameObject.name} [conv:{msj.conversationId}]");

        switch (msj.performativa)
        {
            // Contract Net: el gestor recibe propuestas
            case FIPAPerformativa.CallForProposal:
                ProcesarCFP(msj);
                break;

            case FIPAPerformativa.Propose:
                gestorCN?.RecibirPropuesta(msj);
                break;

            case FIPAPerformativa.RejectProposal:
                Debug.Log($"[{gameObject.name}] Propuesta rechazada por {msj.sender.gameObject.name}");
                break;

            // Contract Net: el contratista recibe asignación
            case FIPAPerformativa.AcceptProposal:
                ProcesarAcceptProposal(msj);
                break;

            // Inform genérico (posición del ladrón compartida)
            case FIPAPerformativa.Inform:
                ProcesarInform(msj);
                break;

            case FIPAPerformativa.InformDone:
                gestorCN?.RecibirInformDone(msj);
                break;

            // Fin de juego (broadcast desde Capturar)
            case FIPAPerformativa.Request when msj.content == "cancelar-contrato":
                historial?.CancelarTodasLasActivas();
                control.RetirarPropuesta(Control.PRIORIDAD_SUBASTA);
                break;

            case FIPAPerformativa.Agree:
                Debug.Log($"[{gameObject.name}] Agree recibido de {msj.sender?.gameObject.name}");
                break;

            case FIPAPerformativa.Refuse:
                Debug.Log($"[{gameObject.name}] Refuse recibido de {msj.sender?.gameObject.name}");
                break;

            case FIPAPerformativa.Failure:
                Debug.Log($"[{gameObject.name}] Failure recibido de {msj.sender?.gameObject.name}");
                break;

            case FIPAPerformativa.NotUnderstood:
                // Evitar responder a un NotUnderstood con otro NotUnderstood
                Debug.LogWarning($"[{gameObject.name}] NotUnderstood recibido de {msj.sender?.gameObject.name}: {msj.content}");
                break;

            // Performativa no reconocida
            default:
                EnviarNotUnderstood(msj);
                break;
        }
    }

    // MANEJADORES DE PERFORMATIVAS

    // Recibe un CFP: decide si puede pujar y envía Propose o Refuse.
    // No puja si ya está persiguiendo al ladrón.
    private void ProcesarCFP(MensajeFIPA cfp)
    {
        if (cfp.contenidoObjeto is not ContenidoCFP contenidoCFP)
        {
            EnviarNotUnderstood(cfp);
            return;
        }

        // Si ya hay una tarea de bloqueo de salida en ejecución, rechazamos
        // Se consulta el historial para saberlo
        if (historial != null && historial.EstaBloqueandoSalida())
        {
            MensajeFIPA refuse = new MensajeFIPA(
                FIPAPerformativa.Refuse,
                this,
                $"(refuse (action {gameObject.name} bloquear-salida) (agente-ya-asignado))",
                cfp.conversationId,
                cfp.protocol);
            refuse.inReplyTo = cfp.replyWith;
            Enviar(cfp.sender, refuse);
            return;
        }

        // Si estamos persiguiendo, nos negamos (Refuse)
        if (modelo.ladronALaVista)
        {
            MensajeFIPA refuse = new MensajeFIPA(
                FIPAPerformativa.Refuse,
                this,
                $"(refuse (action {gameObject.name} bloquear-salida) (agente-ocupado persiguiendo))",
                cfp.conversationId,
                cfp.protocol);
            refuse.inReplyTo = cfp.replyWith;
            Enviar(cfp.sender, refuse);
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
                FIPAPerformativa.Propose,
                this,
                $"(propose (action {gameObject.name} (ir-a (coord {punto.position.x:F1} {punto.position.y:F1} {punto.position.z:F1}))) (= (coste-navegacion) {coste:F1}))",
                cfp.conversationId,
                cfp.protocol);
            propose.contenidoObjeto = oferta;
            propose.inReplyTo       = cfp.replyWith;
            propose.replyWith       = $"propose-{gameObject.name}-{Time.frameCount}";

            Enviar(cfp.sender, propose);
        }
    }

    // Recibe AcceptProposal: activa BloquearSalida hacia el punto asignado
    private void ProcesarAcceptProposal(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is not ContenidoTareaAsignada tarea)
        {
            EnviarNotUnderstood(msj);
            return;
        }
        conversacionBloqueActiva = msj.conversationId;
        Debug.Log($"<color=cyan>[FIPA]</color> {gameObject.name}: tarea aceptada → {tarea.puntoDestino}");

        bloquearSalida.SetPunto(tarea.puntoDestino, msj.sender, msj.conversationId);
        control.RecibirPropuesta(Control.PRIORIDAD_SUBASTA, bloquearSalida);

        // Confirmamos con Agree
        MensajeFIPA agree = new MensajeFIPA(
            FIPAPerformativa.Agree,
            this,
            $"(agree (action {gameObject.name} (ir-a (coord {tarea.puntoDestino.x:F1} {tarea.puntoDestino.y:F1} {tarea.puntoDestino.z:F1}))) true)",
            msj.conversationId,
            msj.protocol);
        agree.inReplyTo = msj.replyWith;
        Enviar(msj.sender, agree);
    }

    // Procesa un Inform genérico según su contenido semántico.
    // Caso actual: posición del ladrón compartida entre guardias.
    private void ProcesarInform(MensajeFIPA msj)
    {
        if (msj.contenidoObjeto is ContenidoInformPosicion info)
        {
            // Actualizamos creencias con la información recibida
            modelo.RegistrarVerLadron(info.posicion, info.llevaElCuadro);
        }
    }

    // Responde con NotUnderstood cuando se recibe una performativa no reconocida.
    // Obligatorio en agentes FIPA-compliant.
    private void EnviarNotUnderstood(MensajeFIPA msj)
    {
        MensajeFIPA nu = new MensajeFIPA(
            FIPAPerformativa.NotUnderstood,
            this,
            $"(not-understood (performativa {msj.performativa}) (razon no-implementada))",
            msj.conversationId);
        nu.inReplyTo = msj.replyWith;
        if (msj.sender != null) Enviar(msj.sender, nu);
    }

    // HELPERS

    // Distancia real navegable por NavMesh hacia un destino
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