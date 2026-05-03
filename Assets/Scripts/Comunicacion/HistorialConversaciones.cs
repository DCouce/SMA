using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class HistorialConversaciones : MonoBehaviour
{
    [Tooltip("Número máximo de mensajes en la cola. Los más antiguos se descartan.")]
    public int capacidadMaxima = 200;

    private readonly Queue<string> cola = new Queue<string>();

    public void RegistrarEnviado(MensajeFIPA msj)  => Encolar(Formatear(msj, "→"));
    public void RegistrarRecibido(MensajeFIPA msj) => Encolar(Formatear(msj, "←"));

    private string Formatear(MensajeFIPA msj, string dir)
    {
        string receiver = msj.receiver == null || msj.receiver.Length == 0
            ? "broadcast"
            : msj.receiver.Length == 1
                ? (msj.receiver[0]?.gameObject.name ?? "?")
                : "broadcast";
        return $"[{Time.time:F1}s] {dir} {msj.performativa} | " +
               $"{msj.sender?.gameObject.name ?? "?"} → {receiver} | " +
               $"conv:{msj.conversationId} | {msj.content}";
    }

    private void Encolar(string linea)
    {
        cola.Enqueue(linea);
        while (cola.Count > capacidadMaxima)
            cola.Dequeue();
    }

    [ContextMenu("Imprimir Historial Completo")]
    public void ImprimirHistorialCompleto()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"\n══════ HISTORIAL DE {gameObject.name} ══════");
        foreach (string linea in cola)
            sb.AppendLine(linea);
        sb.AppendLine("══════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    [ContextMenu("Imprimir Últimos 10")]
    public void ImprimirUltimos10()
    {
        string[] todas = cola.ToArray();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"\n── Últimos 10 mensajes de {gameObject.name} ──");
        for (int i = Mathf.Max(0, todas.Length - 10); i < todas.Length; i++)
            sb.AppendLine(todas[i]);
        Debug.Log(sb.ToString());
    }
}
