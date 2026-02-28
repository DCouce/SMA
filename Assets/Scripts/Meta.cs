using System.ComponentModel;
using UnityEngine;

public class Meta : MonoBehaviour
{
    public GameObject panelVictoria;

    private void OnTriggerEnter(Collider other)
    {
        // Se comprueba si es el jugador
        if (other.CompareTag("Player"))
        {
            PlayerController jugador = other.GetComponent<PlayerController>();

            if (jugador != null && jugador.robado)
            {
                FinalizarPartida();
            }
        }
    }

    private void FinalizarPartida()
    {
        // Se activa el panel de victoria
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }
    }
}