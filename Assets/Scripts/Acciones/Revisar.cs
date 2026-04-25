using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Revisar : MonoBehaviour
{
    private NavMeshAgent agent;
    private Control control;
    private Vector3 destino;
    private float tiempoGiro = 3f;
    private float cronometro = 0;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        control = GetComponent<Control>();
    }

    public void SetDestino(Vector3 posicion){
        destino = posicion;
        if (this.enabled && agent != null)
        {
            agent.destination = destino;
        }
    }

    void OnEnable()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.speed = 0.8f;
            agent.destination = destino;
        }
    }

    void Update()
    {
        // Si llegamos, giramos para simular que buscamos
        if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.3f)
        {   
            StartCoroutine(GirarYFinalizar());
            agent.ResetPath();
        }
    }

    IEnumerator GirarYFinalizar()
    {
        agent.updateRotation = false;
        
        float tiempoFinal = Time.time + 3f;
        
        while (Time.time < tiempoFinal)
        {
            // Esto gira un poquito cada frame
            transform.Rotate(Vector3.up, 150f * Time.deltaTime);
            
            // ESTA LÍNEA ES CLAVE: Le dice a Unity "Dibuja este frame y vuelve aquí en el siguiente"
            yield return null; 
        }

        // Cuando pasan los 3 segundos, salimos del bucle y ejecutamos esto:
        control.RetirarPropuesta(Control.PRIORIDAD_PLANIFICACION);
    }
}