using UnityEngine;
using System;

[Serializable]
public class MensajeFIPA
{
    public string      performativa;
    public Mensajeria  sender;
    public Mensajeria[] receiver;
    public string      content;
    public object      contenidoObjeto;
    public string      protocol;
    public string      conversationId;
    public string      replyWith;
    public string      inReplyTo;

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

    public static string GenerarConversationId()
        => $"conv-{++contadorConversacion}-{Time.frameCount}";

    public override string ToString()
    {
        string recv = receiver != null && receiver.Length > 0
            ? string.Join(", ", Array.ConvertAll(receiver, r => r?.gameObject.name ?? "?"))
            : "broadcast";

        return $"  :performativa {performativa}\n" +
               $"  :sender {sender?.gameObject.name ?? "?"}\n" +
               $"  :receiver (set {recv})\n" +
               $"  :content // {content} //\n" +
               (protocol       != null ? $"  :protocol {protocol}\n"              : "") +
               (conversationId != null ? $"  :conversation-id {conversationId}\n" : "") +
               (replyWith      != null ? $"  :reply-with {replyWith}\n"           : "") +
               (inReplyTo      != null ? $"  :in-reply-to {inReplyTo}\n"          : "") +
               ")";
    }
}

// Tipos de tarea Contract Net
public static class TipoTarea
{
    public const string Bloquear   = "bloquear";
    public const string Investigar = "investigar";
}

[Serializable]
public struct ContenidoCFP
{
    public Vector3 puntoSalida;
    public string  zonaNombre;
    public string  tipoTarea;
}

[Serializable]
public struct ContenidoPropose
{
    public Vector3 puntoDestino;
    public float   costeNavMesh;
}

[Serializable]
public struct ContenidoTareaAsignada
{
    public Vector3 puntoDestino;
    public string  zonaNombre;
    public string  tipoTarea;
}

[Serializable]
public struct ContenidoInformPosicion
{
    public Vector3 posicion;
    public bool    llevaElCuadro;
}

[Serializable]
public struct ContenidoQueryIf
{
    public Vector3 posicionRuido;
}

[Serializable]
public struct ContenidoQueryIfRespuesta
{
    public Vector3 posicionReal;
}

[Serializable]
public struct ContenidoInformCuadroRobado
{
    public Vector3 posicionBase;
}