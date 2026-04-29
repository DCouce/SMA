using UnityEngine;
using System.Collections.Generic;

// Capa de transporte FIPA-ACL del agente guardia.
// Gestiona el envío y recepción de mensajes conforme al estándar FIPA,
// y los delega al componente apropiado según la performativa y el protocolo.
//
// Esta clase es SOLO transporte: no toma decisiones ni gestiona estado.
// ProcesarComunicacion se encarga de interpretar los mensajes.

public class Mensajeria : MonoBehaviour
{
    // Red de agentes registrados
    private static readonly List<Mensajeria> red = new List<Mensajeria>();
    public static IReadOnlyList<Mensajeria> Red => red;

    // Referencias internas
    private GestorContractNet gestorCN;
    private ProcesarComunicacion procesador;
    private HistorialConversaciones historial;
    private Control control;

    void Awake()
    {
        gestorCN   = GetComponent<GestorContractNet>();
        procesador = GetComponent<ProcesarComunicacion>();
        historial  = GetComponent<HistorialConversaciones>();
        control    = GetComponent<Control>();

        red.Add(this);
    }

    void OnDestroy() => red.Remove(this);

    // ═══════════════════════════════════════════════════════
    //  ENVÍO
    // ═══════════════════════════════════════════════════════

    // Envía un mensaje FIPA directamente a un receptor específico.
    public void Enviar(Mensajeria receptor, MensajeFIPA msj)
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
        foreach (Mensajeria agente in red)
        {
            if (agente != this && agente != null)
                agente.Recibir(msj);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  RECEPCIÓN
    // ═══════════════════════════════════════════════════════

    // Punto de entrada de todos los mensajes FIPA recibidos.
    // Delega al componente apropiado según la performativa.
    public void Recibir(MensajeFIPA msj)
    {
        historial?.RegistrarRecibido(msj);

        Debug.Log($"<color=cyan>[FIPA ←]</color> {gameObject.name} recibe {msj.performativa} " +
                  $"de {msj.sender?.gameObject.name} [conv:{msj.conversationId}]");

        switch (msj.performativa)
        {
            // ── Contract Net: rol gestor ──
            case "Propose":
                gestorCN?.RecibirPropuesta(msj);
                break;

            case "InformDone":
                gestorCN?.RecibirInformDone(msj);
                break;

            // ── Contract Net: rol contratista ──
            case "CallForProposal":
                procesador.ProcesarCFP(msj);
                break;

            case "AcceptProposal":
                procesador.ProcesarAcceptProposal(msj);
                break;

            case "RejectProposal":
                Debug.Log($"[{gameObject.name}] Propuesta rechazada por {msj.sender?.gameObject.name}");
                break;

            // ── Inform genérico (posición del ladrón compartida) ──
            case "Inform":
                procesador.ProcesarInform(msj);
                break;

            // ── NUEVO: Inform cuadro robado ──
            case "InformCuadroRobado":
                procesador.ProcesarInformCuadroRobado(msj);
                break;

            // ── NUEVO: QueryIf (¿eras tú el ruido?) ──
            case "QueryIf":
                procesador.ProcesarQueryIf(msj);
                break;

            // ── NUEVO: Confirmación positiva a QueryIf ──
            case "QueryIfConfirm":
                procesador.ProcesarQueryIfConfirm(msj);
                break;

            // ── Fin de juego ──
            case "Request" when msj.content == "cancelar-contrato":
                control.RetirarPropuesta(Control.PRIORIDAD_SUBASTA);
                break;

            // ── Cancel de contrato ──
            case "Cancel":
                procesador.ProcesarCancel(msj);
                break;

            // ── Confirmaciones ──
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
                Debug.LogWarning($"[{gameObject.name}] NotUnderstood recibido de " +
                                 $"{msj.sender?.gameObject.name}: {msj.content}");
                break;

            // ── Performativa no reconocida ──
            default:
                EnviarNotUnderstood(msj);
                break;
        }
    }

    // Responde con NotUnderstood cuando se recibe una performativa no reconocida.
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