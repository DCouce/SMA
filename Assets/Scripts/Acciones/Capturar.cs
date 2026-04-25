using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Capturar : MonoBehaviour
{
    public GameObject panelDerrota;
    private NavMeshAgent agent;
    private bool juegoTerminado = false;
    void Awake() => agent = GetComponent<NavMeshAgent>();

    void OnEnable()
    {
        // Detener al guardia físicamente
        if (agent != null) agent.isStopped = true;

        if (panelDerrota != null) panelDerrota.SetActive(true);
        
        // Se detiene el tiempo para mostrar el panel de derrota
        juegoTerminado = true;

        Debug.Log("¡Capturado!");
    }

    void Update()
    {
        // Detectar si el jugador quiere reiniciar
        if (juegoTerminado && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}