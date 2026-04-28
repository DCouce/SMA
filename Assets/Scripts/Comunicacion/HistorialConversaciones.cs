using UnityEngine;
using System.Collections.Generic;
using System.Text;

// Componente que gestiona el historial completo de conversaciones FIPA
// del agente. Se añade al mismo GameObject que Comunicacion.
//
// Según SC00061G sección 2.5.2, el conversation-id permite a los agentes
// razonar sobre sus conversaciones.
//
// Comunicacion llama a RegistrarEnviado/RegistrarRecibido en cada mensaje.
public class HistorialConversaciones : MonoBehaviour
{
    // Conversaciones indexadas por conversation-id
    private readonly Dictionary<string, Conversacion> conversaciones
        = new Dictionary<string, Conversacion>();

    // REGISTRO DE MENSAJES

    // Registra un mensaje enviado por este agente
    public void RegistrarEnviado(MensajeFIPA msj)
    {
        ObtenerOCrear(msj).RegistrarMensaje(msj, fueEmitido: true);
    }

    // Registra un mensaje recibido por este agente
    public void RegistrarRecibido(MensajeFIPA msj)
    {
        ObtenerOCrear(msj).RegistrarMensaje(msj, fueEmitido: false);
    }

    // CONSULTAS Y RAZONAMIENTO SOBRE EL HISTORIAL

    // Devuelve las conversaciones activas (Iniciada, EnNegociacion, EnEjecucion)
    public List<Conversacion> ConversacionesActivas()
    {
        List<Conversacion> resultado = new List<Conversacion>();
        foreach (Conversacion conv in conversaciones.Values)
        {
            if (conv.estado == EstadoConversacion.Iniciada      ||
                conv.estado == EstadoConversacion.EnNegociacion ||
                conv.estado == EstadoConversacion.EnEjecucion)
            {
                resultado.Add(conv);
            }
        }
        return resultado;
    }

    public List<Conversacion> ConversacionesCompletadas() =>
        FiltrarPorEstado(EstadoConversacion.Completada);

    public List<Conversacion> ConversacionesFallidas() =>
        FiltrarPorEstado(EstadoConversacion.Fallida);

    // Indica si el agente ya está ejecutando una tarea de bloqueo de salida.
    // Lo usa Comunicacion al recibir un CFP para decidir si pujar o rechazar.
    public bool EstaBloqueandoSalida()
    {
        foreach (Conversacion conv in ConversacionesActivas())
        {
            if (conv.protocolo == "fipa-contract-net" &&
                conv.estado == EstadoConversacion.EnEjecucion)
                return true;
        }
        return false;
    }

    // Número total de tareas completadas en esta sesión
    public int TotalTareasCompletadas => ConversacionesCompletadas().Count;

    // Proporción de conversaciones completadas frente al total terminadas.
    // Devuelve 1.0 si aún no hay conversaciones terminadas.
    public float TasaExito()
    {
        int completadas = ConversacionesCompletadas().Count;
        int fallidas    = ConversacionesFallidas().Count;
        int total       = completadas + fallidas;
        return total == 0 ? 1f : (float)completadas / total;
    }

    // CANCELACIÓN

    // Cancela todas las conversaciones activas (al terminar el juego)
    public void CancelarTodasLasActivas()
    {
        foreach (Conversacion conv in ConversacionesActivas())
            conv.estado = EstadoConversacion.Cancelada;
    }

    // LOGGING

    // Imprime el resumen completo de todas las conversaciones del agente.
    [ContextMenu("Imprimir Historial Completo")]
    public void ImprimirHistorialCompleto()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"\n══════ HISTORIAL DE {gameObject.name} ══════");
        sb.AppendLine($"Total conversaciones : {conversaciones.Count}");
        sb.AppendLine($"Activas              : {ConversacionesActivas().Count}");
        sb.AppendLine($"Completadas          : {TotalTareasCompletadas}");
        sb.AppendLine($"Fallidas             : {ConversacionesFallidas().Count}");
        sb.AppendLine($"Tasa de éxito        : {TasaExito():P0}");
        sb.AppendLine();

        foreach (Conversacion conv in conversaciones.Values)
            sb.AppendLine(conv.ResumenCompleto());

        Debug.Log(sb.ToString());
    }

    // HELPERS PRIVADOS

    private Conversacion ObtenerOCrear(MensajeFIPA msj)
    {
        string id = msj.conversationId ?? "sin-id";

        if (!conversaciones.TryGetValue(id, out Conversacion conv))
        {
            conv = new Conversacion(
                id,
                msj.protocol ?? "desconocido",
                msj.sender?.gameObject.name ?? "desconocido");

            conversaciones[id] = conv;
        }
        return conv;
    }

    private List<Conversacion> FiltrarPorEstado(EstadoConversacion estado)
    {
        List<Conversacion> resultado = new List<Conversacion>();
        foreach (Conversacion conv in conversaciones.Values)
            if (conv.estado == estado) resultado.Add(conv);
        return resultado;
    }
}