using UnityEngine;
using System.Collections;

public class Torii : MonoBehaviour
{
    public GameObject pared;
    public float tiempoEspera = 1f; // Tiempo de espera antes de activar la pared
    public float duracion = 3f; // Duración durante la cual la pared estará activa
    private bool procesoActivo = false; // Para evitar múltiples activaciones simultáneas

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !procesoActivo)
        {
            Debug.Log("Bloqueando pared");
            StartCoroutine(ActivarPared());
        }
    }

    IEnumerator ActivarPared()
    {
        if (pared == null) // Verificar si la pared está asignada
        { 
            Debug.LogWarning("Pared no asignada.");
            yield break;
        }

        procesoActivo = true;
        
        yield return new WaitForSeconds(tiempoEspera);
        pared.SetActive(true);
        yield return new WaitForSeconds(duracion);
        pared.SetActive(false);

        procesoActivo = false;
    }
}