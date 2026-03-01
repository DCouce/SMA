using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;

public class PerdidaVision : MonoBehaviour
{
    private Guardia guardia;
    private Investigar investigar;
    private SensorVision sensor;
    private NavMeshAgent agent;

    public Transform cuadro;
    public Transform salida;

    void Start()
    {
        guardia = GetComponent<Guardia>();
        investigar = GetComponent<Investigar>();
        sensor = GetComponent<SensorVision>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void ReaccionarAPerdidaDeVision()
    {
        // Solo generamos nueva ruta si el guardia no está ya moviéndose a un punto de búsqueda
        if (investigar.puntos_investigacion.Count > 0) return;

        if (guardia.sabeRobado)
        {
            Debug.Log("Ladrón perdido CON el cuadro. Asegurando la salida.");
            investigar.radio = 4; 
            investigar.puntos = 5;
            guardia.visto = true;

            agent.SetDestination(sensor.ultimaPosicionConocida);
            if (!agent.pathPending && agent.remainingDistance < 0.5f){
            agent.destination = salida.position;
        
            investigar.GenerateNewPatrolPath(salida.position);
            }
        }
        else
        {
            Debug.Log("Ladrón perdido SIN el cuadro. Revisando el botín.");
            investigar.radio = 2;
            investigar.puntos = 4;
            guardia.visto = true;
            
            agent.SetDestination(sensor.ultimaPosicionConocida);
            if (!agent.pathPending && agent.remainingDistance < 0.5f){
            agent.destination = salida.position;
        
            investigar.GenerateNewPatrolPath(cuadro.position);
            
        }
        }
    }
}