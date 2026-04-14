using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Investigar : MonoBehaviour
{
    public int radio = 3;   
    public int puntos = 3;        
    private List<Vector3> puntos_investigacion = new List<Vector3>();

    private NavMeshAgent agent;
    private Modelado modelo;
    private Control control;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        modelo = GetComponent<Modelado>();
        control = GetComponent<Control>();
    }

    // La Capa de Planificación llama a esto ANTES de proponer la acción
    public void SetPuntoRuido(Vector3 posicion)
    {
        puntos_investigacion.Clear();
        for (int i = 0; i < puntos; i++)
        {
            puntos_investigacion.Add(GetValidNavMeshPoint(posicion, radio));
        }
    }

    void OnEnable()
    {
        if(puntos_investigacion.Count > 0) agent.SetDestination(puntos_investigacion[0]);
    }

    void Update()
    {
        if (puntos_investigacion.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.7f) 
        {
            puntos_investigacion.RemoveAt(0);
            
            if (puntos_investigacion.Count > 0) 
                agent.SetDestination(puntos_investigacion[0]);
            else 
            {
                // ¡HE TERMINADO! Limpiamos memoria y nos retiramos
                modelo.MarcarRuidoComoAtendido();
                control.RetirarPropuesta(Control.PRIORIDAD_PLANIFICACION);
            }
        }
    }

    private Vector3 GetValidNavMeshPoint(Vector3 center, int range)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, range, NavMesh.AllAreas)) return hit.position;
        }
        return center; 
    }
}