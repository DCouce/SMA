using UnityEngine;
using UnityEngine.AI;

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

    public void SetDestino(Vector3 posicion) => destino = posicion;

    void OnEnable()
    {
        agent.destination = destino;
        agent.speed = 0.8f;
        cronometro = tiempoGiro;
    }

    void Update()
    {
        // Si llegamos, giramos para simular que buscamos
        if (!agent.pathPending && agent.remainingDistance < 0.3f)
        {   
            transform.Rotate(Vector3.up, 150f * Time.deltaTime);
            cronometro -= Time.deltaTime;

            if (cronometro <= 0) 
                control.RetirarPropuesta(Control.PRIORIDAD_PLANIFICACION);
        }
    }
}