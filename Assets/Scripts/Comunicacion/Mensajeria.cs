using UnityEngine;
using System.Collections.Generic;

public class Mensajeria : MonoBehaviour
{
    private static readonly List<Mensajeria> red = new List<Mensajeria>();
    public static IReadOnlyList<Mensajeria> Red => red;

    public event System.Action<Mensajeria> OnCFPRecibido;

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

    // Envía un mensaje FIPA directamente a un receptor específico.
    public void Enviar(Mensajeria receptor, MensajeFIPA msj)
    {
        msj.sender   = this;
        msj.receiver = new[] { receptor };

        historial?.RegistrarEnviado(msj);
        Debug.Log($"<color=green>[FIPA →]</color> {msj}");
        receptor.Recibir(msj);
    }

    // Difunde un mensaje a todos los agentes de la red excepto a sí mismo.
    public void Difundir(MensajeFIPA msj)
    {
        msj.sender = this;

        List<Mensajeria> destinatarios = new List<Mensajeria>();
        foreach (Mensajeria agente in red)
            if (agente != this && agente != null)
                destinatarios.Add(agente);

        msj.receiver = destinatarios.ToArray();

        historial?.RegistrarEnviado(msj);
        Debug.Log($"<color=lime>[FIPA ↦]</color> {msj}");
        foreach (Mensajeria agente in msj.receiver)
            agente.Recibir(msj);
    }

    // Entrada de todos los mensajes FIPA recibidos.
    public void Recibir(MensajeFIPA msj)
    {
        historial?.RegistrarRecibido(msj);

        Debug.Log($"<color=cyan>[FIPA ←]</color> {gameObject.name} recibe {msj.performativa} " +
                  $"de {msj.sender?.gameObject.name} [conv:{msj.conversationId}]");

        switch (msj.performativa)
        {
            // Contract Net: rol gestor
            case "propose":
                gestorCN?.RecibirPropuesta(msj);
                break;

            // Contract Net: rol contratista
            case "cfp":
                OnCFPRecibido?.Invoke(msj.sender);
                if (procesador != null)
                    procesador.ProcesarCFP(msj);
                else
                    procesador?.EnviarRefuse(msj, "(agente-no-contratista)");
                break;

            case "accept-proposal":
                procesador?.ProcesarAcceptProposal(msj);
                break;

            case "reject-proposal":
                Debug.Log($"[{gameObject.name}] reject-proposal de {msj.sender?.gameObject.name}");
                break;

            // Inform: siguiendo ontología.
            case "inform":
                if (msj.ontology == "robo")
                    procesador?.ProcesarInformCuadroRobado(msj);
                else if (msj.ontology == "posicion-ladron")
                    procesador?.ProcesarInform(msj);
                break;

            case "inform-done":
                gestorCN?.RecibirInformDone(msj);
                break;

            case "query-if-confirm":
                procesador?.ProcesarQueryIfConfirm(msj);
                break;

            // QueryIf (identificación de ruido)
            case "query-if":
                procesador?.ProcesarQueryIf(msj);
                break;

            // Fin de juego: cancelar contratos activos como gestor y liberar rol contratista
            case "request" when msj.content == "cancelar-contrato":
                gestorCN?.CancelarTodosLosContratos("juego-terminado");
                control?.RetirarPropuesta(Control.PRIORIDAD_SUBASTA);
                break;

            // Cancelar contrato
            case "cancel":
                procesador?.ProcesarCancel(msj);
                break;

            // Confirmaciones
            case "agree":
                Debug.Log($"[{gameObject.name}] agree de {msj.sender?.gameObject.name}");
                break;

            case "refuse":
                Debug.Log($"[{gameObject.name}] refuse de {msj.sender?.gameObject.name}");
                break;

            case "failure":
                Debug.Log($"[{gameObject.name}] failure de {msj.sender?.gameObject.name}");
                break;

            case "not-understood":
                Debug.LogWarning($"[{gameObject.name}] not-understood de " +
                                 $"{msj.sender?.gameObject.name}: {msj.content}");
                break;

            // Performativa no reconocida
            default:
                EnviarNotUnderstood(msj);
                break;
        }
    }

    // Responde con NotUnderstood cuando se recibe una performativa no reconocida.
    public void EnviarNotUnderstood(MensajeFIPA msj)
    {
        MensajeFIPA nu = new MensajeFIPA(
            "not-understood",
            this,
            $"(performativa {msj.performativa}) (razon no-implementada)",
            msj.conversationId);
        nu.inReplyTo = msj.replyWith;
        if (msj.sender != null) Enviar(msj.sender, nu);
    }
}