using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Revisar : MonoBehaviour
{
    private NavMeshAgent agent;
    private Control control;
    private Vector3 destino;
    private float tiempoGiro = 3f;

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
            agent.ResetPath();
            StartCoroutine(GirarYFinalizar());
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (agent != null) agent.updateRotation = true;
    }

    IEnumerator GirarYFinalizar()
    {
        agent.updateRotation = false;

        float tiempoFinal = Time.time + tiempoGiro;

        while (Time.time < tiempoFinal)
        {
            transform.Rotate(Vector3.up, 80f * Time.deltaTime);
            yield return null;
        }

        agent.updateRotation = true;
        control.RetirarPropuesta(Control.PRIORIDAD_PLANIFICACION);
    }
}