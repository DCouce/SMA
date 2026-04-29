using UnityEngine;
using System;

[Serializable]
public class MensajeFIPA
{
    // Performativa del mensaje (tipo de acto comunicativo)
    public string performativa;

    // Agente que envía el mensaje
    public Mensajeria sender;

    // Agentes receptores. Null o vacío = broadcast a todos los agentes.
    public Mensajeria[] receiver;

    // Contenido semántico del mensaje en FIPA-SL (formato textual)
    public string content;

    // Objeto C# que representa el contenido tipado (posición, oferta, etc.)
    public object contenidoObjeto;

    // Protocolo de interacción seguido (p.ej. "fipa-contract-net", "fipa-request")
    public string protocol;

    // Identificador único de la conversación
    public string conversationId;

    // Etiqueta de este mensaje (el receptor la usa en inReplyTo para responder)
    public string replyWith;

    // Etiqueta del mensaje al que este es respuesta
    public string inReplyTo;

    // Constructor con los campos mínimos necesarios
    public MensajeFIPA(
        string performativa,
        Mensajeria sender,
        string content,
        string conversationId = null,
        string protocol = null)
    {
        this.performativa   = performativa;
        this.sender         = sender;
        this.content        = content;
        this.conversationId = conversationId ?? GenerarConversationId();
        this.protocol       = protocol;
    }

    private static int contadorConversacion = 0;

    // Genera un conversation-id único para esta sesión
    public static string GenerarConversationId()
    {
        return $"conv-{++contadorConversacion}-{Time.frameCount}";
    }

    // Representación al estilo FIPA-ACL para logging
    public override string ToString()
    {
        string recv = receiver != null && receiver.Length > 0
            ? string.Join(", ", Array.ConvertAll(receiver, r => r?.gameObject.name ?? "?"))
            : "broadcast";

        return $"  :performativa {performativa}\n" +
               $"  :sender {sender?.gameObject.name ?? "?"}\n" +
               $"  :receiver (set {recv})\n" +
               $"  :content \"{content}\"\n" +
               (protocol       != null ? $"  :protocol {protocol}\n"             : "") +
               (conversationId != null ? $"  :conversation-id {conversationId}\n" : "") +
               (replyWith      != null ? $"  :reply-with {replyWith}\n"          : "") +
               (inReplyTo      != null ? $"  :in-reply-to {inReplyTo}\n"         : "") +
               ")";
    }
}

// ═══════════════════════════════════════════════════════
//  Contenidos tipados que viajan en contenidoObjeto
// ═══════════════════════════════════════════════════════

// Contenido de un CFP: UN solo punto de salida a cubrir (un CN por tarea)
[Serializable]
public struct ContenidoCFP
{
    public Vector3 puntoSalida;
    public string zonaNombre;
}

// Contenido de un Propose: oferta de un agente contratista
[Serializable]
public struct ContenidoPropose
{
    public Vector3 puntoDestino;
    public float costeNavMesh;
}

// Contenido de un AcceptProposal: tarea concreta asignada al ganador
[Serializable]
public struct ContenidoTareaAsignada
{
    public Vector3 puntoDestino;
    public string zonaNombre;
}

// Contenido de un Inform para compartir la posición del ladrón
[Serializable]
public struct ContenidoInformPosicion
{
    public Vector3 posicion;
    public bool llevaElCuadro;
}