// Estado actual de una conversación FIPA según el protocolo que sigue.
public enum EstadoConversacion
{
    // El iniciador envió el CFP o Request. Esperando respuestas.
    Iniciada,

    // Se han recibido propuestas o agree. En proceso de evaluación.
    EnNegociacion,

    // Se ha aceptado una propuesta. El contratista está ejecutando la tarea.
    EnEjecucion,

    // La tarea fue completada con éxito (InformDone recibido).
    Completada,

    // La conversación terminó con fallo (Failure o Refuse definitivo).
    Fallida,

    // La conversación fue cancelada por el iniciador.
    Cancelada,

    // La conversación expiró sin completarse (deadline superado).
    Expirada
}