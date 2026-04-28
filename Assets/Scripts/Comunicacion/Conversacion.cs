using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

// Registro de un mensaje individual dentro de una conversación.
// Combina el mensaje FIPA con metadatos de recepción.
[Serializable]
public class EntradaHistorial
{
    public MensajeFIPA mensaje;
    public float timestamp;
    public bool fueEmitido;

    public EntradaHistorial(MensajeFIPA mensaje, bool fueEmitido)
    {
        this.mensaje    = mensaje;
        this.fueEmitido = fueEmitido;
        this.timestamp  = Time.time;
    }

    public override string ToString()
    {
        string rol = fueEmitido ? "ENVIADO" : "RECIBIDO";
        return $"[t={timestamp:F1}s] [{rol}] {mensaje.performativa} " +
               $"| conv:{mensaje.conversationId} | {mensaje.content}";
    }
}

// Representa una conversación completa entre agentes FIPA.
// Almacena todos sus mensajes ordenados cronológicamente y su estado actual.
// Según SC00061G, el conversation-id permite a los agentes razonar sobre
// el historial de sus conversaciones.
[Serializable]
public class Conversacion
{
    // Identificación
    public string conversationId;
    public string protocolo;
    public string iniciadorNombre;

    // Estado
    public EstadoConversacion estado;
    public float timestampInicio;
    public float timestampUltimoMensaje;

    // Mensajes en orden cronológico
    public List<EntradaHistorial> mensajes = new List<EntradaHistorial>();

    public Conversacion(string conversationId, string protocolo, string iniciadorNombre)
    {
        this.conversationId         = conversationId;
        this.protocolo              = protocolo;
        this.iniciadorNombre        = iniciadorNombre;
        this.estado                 = EstadoConversacion.Iniciada;
        this.timestampInicio        = Time.time;
        this.timestampUltimoMensaje = Time.time;
    }

    // Añade un mensaje al historial y actualiza el estado de la conversación
    public void RegistrarMensaje(MensajeFIPA msj, bool fueEmitido)
    {
        mensajes.Add(new EntradaHistorial(msj, fueEmitido));
        timestampUltimoMensaje = Time.time;
        ActualizarEstado(msj.performativa);
    }

    // Actualiza el estado siguiendo el flujo del FIPA Contract Net IP
    private void ActualizarEstado(FIPAPerformativa performativa)
    {
        switch (performativa)
        {
            case FIPAPerformativa.CallForProposal:
            case FIPAPerformativa.Request:
                estado = EstadoConversacion.Iniciada;
                break;

            case FIPAPerformativa.Propose:
            case FIPAPerformativa.Agree:
                estado = EstadoConversacion.EnNegociacion;
                break;

            case FIPAPerformativa.AcceptProposal:
                estado = EstadoConversacion.EnEjecucion;
                break;

            case FIPAPerformativa.InformDone:
                estado = EstadoConversacion.Completada;
                break;

            case FIPAPerformativa.Failure:
            case FIPAPerformativa.RejectProposal:
            case FIPAPerformativa.Refuse:
                // Solo es fallo definitivo si no hay otros participantes activos
                if (estado != EstadoConversacion.EnEjecucion)
                    estado = EstadoConversacion.Fallida;
                break;
        }
    }

    public float Duracion    => timestampUltimoMensaje - timestampInicio;
    public int TotalMensajes => mensajes.Count;

    // Genera un resumen legible de la conversación completa
    public string ResumenCompleto()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== CONVERSACIÓN {conversationId} ===");
        sb.AppendLine($"Protocolo : {protocolo}");
        sb.AppendLine($"Iniciador : {iniciadorNombre}");
        sb.AppendLine($"Estado    : {estado}");
        sb.AppendLine($"Duración  : {Duracion:F1}s  |  Mensajes: {TotalMensajes}");
        sb.AppendLine("--- Mensajes ---");
        foreach (EntradaHistorial entrada in mensajes)
            sb.AppendLine(entrada.ToString());
        sb.AppendLine("=====================================");
        return sb.ToString();
    }
}