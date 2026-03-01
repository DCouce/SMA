using UnityEngine;

public class Capturar : MonoBehaviour
{
    [Header("Configuración de Interfaz")]
    public GameObject panelDerrota;

    public void EjecutarCaptura()
    {
        // Panel de derrota
        if (panelDerrota != null)
        {
            panelDerrota.SetActive(true);
        }

    }
}