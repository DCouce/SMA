using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class Comunicacion : MonoBehaviour
{
    private static readonly List<Comunicacion> todosLosAgentes = new List<Comunicacion>();

    private Modelado       modelo;
    private Control        control;
    private NavMeshAgent   navAgent;
    private BloquearSalida bloquearSalida;
    private Subastador     subastador;

    void Awake()
    {
        modelo        = GetComponent<Modelado>();
        control       = GetComponent<Control>();
        navAgent      = GetComponent<NavMeshAgent>();
        bloquearSalida = GetComponent<BloquearSalida>();
        subastador    = GetComponent<Subastador>();

        todosLosAgentes.Add(this);
    }

    void OnDestroy()
    {
        todosLosAgentes.Remove(this);
    }

    // Envía un mensaje a todos los agentes de la red excepto a uno mismo
    public void DifundirMensaje(TipoMensaje tipo, object datos)
    {
        Debug.Log($"<color=green>[RADIO]</color> {gameObject.name} difunde {tipo}. Agentes en red: {todosLosAgentes.Count}");
        MensajeAgente msj = new MensajeAgente { emisor = this, tipo = tipo, contenido = datos };
        foreach (var receptor in todosLosAgentes)
            if (receptor != this && receptor != null)
                EnviarMensajeDirecto(receptor, msj);
    }

    public void RecibirMensaje(MensajeAgente msj)
    {
        switch (msj.tipo)
        {
            case TipoMensaje.Subasta_Abierta:
                ProcesarSubasta((List<Transform>)msj.contenido, msj.emisor);
                break;

            case TipoMensaje.Oferta_Enviada:
                subastador.RegistrarOferta(msj);
                break;

            case TipoMensaje.Tarea_Asignada:
                Vector3 puntoDestino = (Vector3)msj.contenido;
                Debug.Log($"<color=cyan>[SUBASTA]</color> {gameObject.name}: tarea asignada en {puntoDestino}");
                bloquearSalida.SetPunto(puntoDestino);
                control.RecibirPropuesta(Control.PRIORIDAD_SUBASTA, bloquearSalida);
                break;

            case TipoMensaje.Juego_Terminado:
                control.RetirarPropuesta(Control.PRIORIDAD_SUBASTA);
                break;
        }
    }

    private void ProcesarSubasta(List<Transform> puntos, Comunicacion subastadorRemoto)
    {
        // No ofertamos si estamos persiguiendo: no podemos bloquear nada útil
        if (modelo.ladronALaVista) return;

        foreach (var punto in puntos)
        {
            float coste = CalcularCosteNavMesh(punto.position);
            MensajeAgente msj = new MensajeAgente
            {
                emisor   = this,
                tipo     = TipoMensaje.Oferta_Enviada,
                contenido = new Oferta { puntoDestino = punto.position, valor = coste }
            };
            EnviarMensajeDirecto(subastadorRemoto, msj);
        }
    }

    // Distancia real navegable por NavMesh (más precisa que distancia euclídea)
    private float CalcularCosteNavMesh(Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        if (navAgent.CalculatePath(destino, path))
        {
            float distancia = 0;
            for (int i = 0; i < path.corners.Length - 1; i++)
                distancia += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            return distancia;
        }
        return float.MaxValue;
    }

    public void EnviarMensajeDirecto(Comunicacion receptor, MensajeAgente msj)
    {
        receptor.RecibirMensaje(msj);
    }
}
