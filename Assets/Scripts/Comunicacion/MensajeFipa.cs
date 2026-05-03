using UnityEngine;
using System;
using System.Globalization;

[Serializable]
public class MensajeFIPA
{
    public string      performativa;
    public Mensajeria  sender;
    public Mensajeria[] receiver;
    public string      content;
    public string      ontology;
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

        return $"(\n" +
               $"  :performativa {performativa}\n" +
               $"  :sender {sender?.gameObject.name ?? "?"}\n" +
               $"  :receiver (set {recv})\n" +
               $"  :content // {content} //\n" +
               (ontology       != null ? $"  :ontology {ontology}\n"              : "") +
               (protocol       != null ? $"  :protocol {protocol}\n"              : "") +
               (conversationId != null ? $"  :conversation-id {conversationId}\n" : "") +
               (replyWith      != null ? $"  :reply-with {replyWith}\n"           : "") +
               (inReplyTo      != null ? $"  :in-reply-to {inReplyTo}\n"          : "") +
               ")";
    }

    private static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);

    // "tipo//x//y//z//zona"  →  cfp / accept-proposal
    public static string ContentCFP(string tipo, Vector3 p, string zona)
        => $"{tipo}//{F(p.x)}//{F(p.y)}//{F(p.z)}//{zona}";

    // "x//y//z//cost"  →  propose
    public static string ContentPropose(Vector3 p, float cost)
        => $"{F(p.x)}//{F(p.y)}//{F(p.z)}//{F(cost)}";

    // "x//y//z"  →  agree / inform-done / query-if / inform cuadro
    public static string ContentVec3(Vector3 p)
        => $"{F(p.x)}//{F(p.y)}//{F(p.z)}";

    // "x//y//z//true|false"  →  inform posicion-ladron
    public static string ContentInformPosicion(Vector3 p, bool llevaElCuadro)
        => $"{F(p.x)}//{F(p.y)}//{F(p.z)}//{llevaElCuadro.ToString().ToLower()}";

    // "agente//x//y//z"  →  query-if-confirm
    public static string ContentQueryIfConfirm(string agente, Vector3 p)
        => $"{agente}//{F(p.x)}//{F(p.y)}//{F(p.z)}";


    // Parsear content
    private static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    private static string[] Partes(string content)
        => content.Split(new[] { "//" }, StringSplitOptions.None);

    // "tipo//x//y//z//zona"
    public static (string tipo, Vector3 punto, string zona) ParseCFP(string content)
    {
        string[] p = Partes(content);
        return (p[0],
                new Vector3(P(p[1]), P(p[2]), P(p[3])),
                p.Length > 4 ? p[4] : "");
    }

    // "x//y//z//cost"
    public static (Vector3 punto, float cost) ParsePropose(string content)
    {
        string[] p = Partes(content);
        return (new Vector3(P(p[0]), P(p[1]), P(p[2])), P(p[3]));
    }

    // "x//y//z//true|false"
    public static (Vector3 posicion, bool llevaElCuadro) ParseInformPosicion(string content)
    {
        string[] p = Partes(content);
        return (new Vector3(P(p[0]), P(p[1]), P(p[2])), bool.Parse(p[3]));
    }

    // "x//y//z"
    public static Vector3 ParseVec3(string content)
    {
        string[] p = Partes(content);
        return new Vector3(P(p[0]), P(p[1]), P(p[2]));
    }

    // "agente//x//y//z"
    public static (string agente, Vector3 posicion) ParseQueryIfConfirm(string content)
    {
        string[] p = Partes(content);
        return (p[0], new Vector3(P(p[1]), P(p[2]), P(p[3])));
    }
}

// Tipos de tarea Contract Net
public static class TipoTarea
{
    public const string Bloquear   = "bloquear";
    public const string Investigar = "investigar";
}
