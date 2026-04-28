public enum FIPAPerformativa
{
    // ACTOS PRIMITIVOS

    // El emisor informa al receptor de que una proposición es verdadera.
    Inform,

    // El emisor solicita al receptor que realice una acción.
    Request,

    // PROTOCOLO Contract Net

    // Call For Proposal: inicia una negociación solicitando propuestas
    // para realizar una acción bajo ciertas condiciones.
    CallForProposal,

    // El emisor presenta una propuesta para realizar la acción.
    Propose,

    // El emisor acepta la propuesta enviada por el receptor.
    AcceptProposal,

    // El emisor rechaza la propuesta enviada por el receptor.
    RejectProposal,

    // RESPUESTAS DE GESTIÓN

    // El emisor acuerda realizar la acción solicitada.
    Agree,

    // El emisor rechaza realizar la acción solicitada.
    Refuse,

    // El emisor informa de que no pudo completar la acción.
    Failure,

    // El receptor no entendió el mensaje. Obligatorio en agentes FIPA-compliant.
    NotUnderstood,

    // El emisor informa de que la acción solicitada ha sido completada.
    InformDone,
}