using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Subastador : MonoBehaviour
{
    private Comunicacion comms;
    private List<MensajeAgente> ofertasRecibidas = new List<MensajeAgente>();
    private bool subastaActiva = false;

    void Awake()
    {
        comms = GetComponent<Comunicacion>();
    }

    // Llamado por CapaReactiva cuando ve al ladrón
    public void PrepararSubasta(List<Transform> puntos)
    {
        if (subastaActiva) return;
        ofertasRecibidas.Clear();
        StartCoroutine(EsperarYAsignar(puntos));
    }

    // Llamado por Comunicacion cuando llega una oferta por radio
    public void RegistrarOferta(MensajeAgente msj)
    {
        if (subastaActiva)
            ofertasRecibidas.Add(msj);
    }

    private IEnumerator EsperarYAsignar(List<Transform> puntos)
    {
        subastaActiva = true;

        // Difundir la subasta a todos los demás agentes
        comms.DifundirMensaje(TipoMensaje.Subasta_Abierta, puntos);

        // Tiempo para que los agentes calculen su NavMesh y envíen su oferta
        yield return new WaitForSeconds(0.2f);

        foreach (Transform punto in puntos)
            AsignarPuntoAlMejor(punto.position);

        subastaActiva = false;
    }

    private void AsignarPuntoAlMejor(Vector3 posicionInterseccion)
    {
        Comunicacion mejorAgente = null;
        float menorCoste = float.MaxValue;

        foreach (var msj in ofertasRecibidas)
        {
            Oferta oferta = (Oferta)msj.contenido;
            if (oferta.puntoDestino == posicionInterseccion && oferta.valor < menorCoste)
            {
                menorCoste = oferta.valor;
                mejorAgente = msj.emisor;
            }
        }

        if (mejorAgente != null)
        {
            Debug.Log($"<color=cyan>[SUBASTA]</color> {gameObject.name} asigna {posicionInterseccion} a {mejorAgente.gameObject.name} (coste {menorCoste:F1}m)");
            MensajeAgente msj = new MensajeAgente
            {
                emisor   = comms,
                tipo     = TipoMensaje.Tarea_Asignada,
                contenido = posicionInterseccion
            };
            comms.EnviarMensajeDirecto(mejorAgente, msj);
            // Evitar asignar dos puntos al mismo agente
            ofertasRecibidas.RemoveAll(m => m.emisor == mejorAgente);
        }
    }
}
