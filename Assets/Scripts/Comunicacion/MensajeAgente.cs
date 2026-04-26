using UnityEngine;

public enum TipoMensaje
{
    Subasta_Abierta,
    Oferta_Enviada,
    Tarea_Asignada,
    Juego_Terminado
}

public struct MensajeAgente
{
    public Comunicacion emisor;
    public TipoMensaje tipo;
    public object contenido;
}

[System.Serializable]
public struct Oferta
{
    public Vector3 puntoDestino;
    public float valor;
}
