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
        if (agent != null) agent.isStopped = true;
        if (panelDerrota != null) panelDerrota.SetActive(true);
        juegoTerminado = true;

        // Notificar a todos los guardias para que liberen sus asignaciones
        var msj = new MensajeFIPA("Request",
        GetComponent<Mensajeria>(), "cancelar-contrato");

        GetComponent<Mensajeria>().Difundir(msj);        

        Debug.Log("¡Capturado!");
    }

    void Update()
    {
        if (juegoTerminado && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
