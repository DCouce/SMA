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
    private ProcesarComunicacion procesador;
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
        procesador     = GetComponent<ProcesarComunicacion>();

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
            case "CallForProposal":
                procesador.ProcesarCFP(msj);
                break;

            case "Propose":
                gestorCN?.RecibirPropuesta(msj);
                break;

            case "RejectProposal":
                Debug.Log($"[{gameObject.name}] Propuesta rechazada por {msj.sender.gameObject.name}");
                break;

            // Contract Net: el contratista recibe asignación
            case "AcceptProposal":
                procesador.ProcesarAcceptProposal(msj);
                break;

            // Inform genérico (posición del ladrón compartida)
            case "Inform":
                procesador.ProcesarInform(msj);
                break;

            case "InformDone":
                gestorCN?.RecibirInformDone(msj);
                break;

            // Fin de juego (broadcast desde Capturar)
            case "Request" when msj.content == "cancelar-contrato":
                historial?.CancelarTodasLasActivas();
                control.RetirarPropuesta(Control.PRIORIDAD_SUBASTA);
                break;

            case "Agree":
                Debug.Log($"[{gameObject.name}] Agree recibido de {msj.sender?.gameObject.name}");
                break;

            case "Refuse":
                Debug.Log($"[{gameObject.name}] Refuse recibido de {msj.sender?.gameObject.name}");
                break;

            case "Failure":
                Debug.Log($"[{gameObject.name}] Failure recibido de {msj.sender?.gameObject.name}");
                break;

            case "NotUnderstood":
                // Evitar responder a un NotUnderstood con otro NotUnderstood
                Debug.LogWarning($"[{gameObject.name}] NotUnderstood recibido de {msj.sender?.gameObject.name}: {msj.content}");
                break;

            // Performativa no reconocida
            default:
                EnviarNotUnderstood(msj);
                break;
        }
    }



    // Responde con NotUnderstood cuando se recibe una performativa no reconocida.
    // Obligatorio en agentes FIPA-compliant.
    public void EnviarNotUnderstood(MensajeFIPA msj)
    {
        MensajeFIPA nu = new MensajeFIPA(
            "NotUnderstood",
            this,
            $"(not-understood (performativa {msj.performativa}) (razon no-implementada))",
            msj.conversationId);
        nu.inReplyTo = msj.replyWith;
        if (msj.sender != null) Enviar(msj.sender, nu);
    }
}