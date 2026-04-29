using UnityEngine;
using System.Collections.Generic;
using System.Text;

// Historial de mensajes FIPA del agente, implementado como una cola (FIFO).
// Registra todos los mensajes enviados y recibidos en orden cronológico.
// No gestiona estados de conversación: es un log puro para consulta y depuración.
//
// Se puede consultar desde el Inspector con [ContextMenu] o desde código.

public class HistorialConversaciones : MonoBehaviour
{
    [Tooltip("Número máximo de mensajes en la cola. Los más antiguos se descartan.")]
    public int capacidadMaxima = 200;

    // Cola FIFO de entradas
    private readonly Queue<EntradaMensaje> cola = new Queue<EntradaMensaje>();

    // ═══════════════════════════════════════════════════════
    //  Entrada individual del historial
    // ═══════════════════════════════════════════════════════

    [System.Serializable]
    public class EntradaMensaje
    {
        public string performativa;
        public string sender;
        public string receiver;
        public string content;
        public string conversationId;
        public string protocol;
        public float  timestamp;
        public bool   fueEmitido;

        public EntradaMensaje(MensajeFIPA msj, bool fueEmitido)
        {
            this.performativa   = msj.performativa;
            this.sender         = msj.sender?.gameObject.name ?? "?";
            this.receiver       = msj.receiver != null && msj.receiver.Length > 0
                                    ? msj.receiver[0]?.gameObject.name ?? "?"
                                    : "broadcast";
            this.content        = msj.content;
            this.conversationId = msj.conversationId;
            this.protocol       = msj.protocol;
            this.timestamp      = Time.time;
            this.fueEmitido     = fueEmitido;
        }

        public override string ToString()
        {
            string dir = fueEmitido ? "→" : "←";
            return $"[{timestamp:F1}s] {dir} {performativa} | " +
                   $"{sender} → {receiver} | conv:{conversationId} | {content}";
        }
    }

    // ═══════════════════════════════════════════════════════
    //  REGISTRO
    // ═══════════════════════════════════════════════════════

    public void RegistrarEnviado(MensajeFIPA msj)
    {
        Encolar(new EntradaMensaje(msj, fueEmitido: true));
    }

    public void RegistrarRecibido(MensajeFIPA msj)
    {
        Encolar(new EntradaMensaje(msj, fueEmitido: false));
    }

    private void Encolar(EntradaMensaje entrada)
    {
        cola.Enqueue(entrada);

        // Descartar los más antiguos si superamos la capacidad
        while (cola.Count > capacidadMaxima)
            cola.Dequeue();
    }

    // ═══════════════════════════════════════════════════════
    //  CONSULTAS
    // ═══════════════════════════════════════════════════════

    // Número total de mensajes en el historial
    public int TotalMensajes => cola.Count;

    // Devuelve todos los mensajes como array (orden cronológico)
    public EntradaMensaje[] ObtenerTodos()
    {
        return cola.ToArray();
    }

    // Devuelve los últimos N mensajes
    public List<EntradaMensaje> ObtenerUltimos(int n)
    {
        EntradaMensaje[] todos = cola.ToArray();
        List<EntradaMensaje> resultado = new List<EntradaMensaje>();
        int inicio = Mathf.Max(0, todos.Length - n);
        for (int i = inicio; i < todos.Length; i++)
            resultado.Add(todos[i]);
        return resultado;
    }

    // Filtra mensajes por conversation-id
    public List<EntradaMensaje> ObtenerPorConversacion(string conversationId)
    {
        List<EntradaMensaje> resultado = new List<EntradaMensaje>();
        foreach (EntradaMensaje e in cola)
        {
            if (e.conversationId == conversationId)
                resultado.Add(e);
        }
        return resultado;
    }

    // Filtra mensajes por performativa
    public List<EntradaMensaje> ObtenerPorPerformativa(string performativa)
    {
        List<EntradaMensaje> resultado = new List<EntradaMensaje>();
        foreach (EntradaMensaje e in cola)
        {
            if (e.performativa == performativa)
                resultado.Add(e);
        }
        return resultado;
    }

    // ═══════════════════════════════════════════════════════
    //  LOGGING (accesible desde el Inspector)
    // ═══════════════════════════════════════════════════════

    [ContextMenu("Imprimir Historial Completo")]
    public void ImprimirHistorialCompleto()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"\n══════ HISTORIAL DE {gameObject.name} ══════");
        sb.AppendLine($"Total mensajes: {cola.Count}");
        sb.AppendLine();

        foreach (EntradaMensaje entrada in cola)
            sb.AppendLine(entrada.ToString());

        sb.AppendLine("══════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    [ContextMenu("Imprimir Últimos 10")]
    public void ImprimirUltimos10()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"\n── Últimos 10 mensajes de {gameObject.name} ──");
        foreach (EntradaMensaje entrada in ObtenerUltimos(10))
            sb.AppendLine(entrada.ToString());
        Debug.Log(sb.ToString());
    }

    // Limpia todo el historial
    public void Limpiar()
    {
        cola.Clear();
    }
}