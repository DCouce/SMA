using UnityEngine;
using UnityEngine.AI;

public class Capturar : MonoBehaviour
{
    public GameObject panelDerrota;
    private NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void OnEnable()
    {
        // Detener al guardia físicamente
        if (agent != null) agent.isStopped = true;

        if (panelDerrota != null) panelDerrota.SetActive(true);
        
        Debug.Log("¡Capturado!");
        // Aquí podrías congelar el tiempo: Time.timeScale = 0;
    }
}