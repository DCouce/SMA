using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class Investigar : MonoBehaviour
{
    public int radio = 3;   
    public int puntos = 3;        
    public List<Vector3> puntos_investigacion = new List<Vector3>();

    private NavMeshAgent agent;
    private Guardia guardia;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        guardia = GetComponent<Guardia>();
    }

    public void GenerateNewPatrolPath(Vector3 posicion)
    {
        puntos_investigacion.Clear();
        for (int i = 0; i < puntos; i++)
        {
            Vector3 randomPoint = GetValidNavMeshPoint(posicion, radio);
            puntos_investigacion.Add(randomPoint);
        }
        if(puntos_investigacion.Count > 0) agent.SetDestination(puntos_investigacion[0]);
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

    public void Investigacion()
    {
        if (puntos_investigacion.Count == 0) return;

        if (!agent.pathPending && agent.remainingDistance < 0.7f) 
        {
            puntos_investigacion.RemoveAt(0);
            
            if (puntos_investigacion.Count > 0) 
            {
                agent.SetDestination(puntos_investigacion[0]);
            }
            else 
            {
                // Si ya no quedan puntos, avisamos al cerebro
                guardia.FinalizarInvestigacionRuido();
            }
        }
    }

    void OnDrawGizmos() // Para ver los puntos generados
    {
        if (puntos_investigacion == null) return;
        Gizmos.color = Color.cyan;
        foreach (Vector3 p in puntos_investigacion)
        {
            Gizmos.DrawSphere(p, 0.5f);
        }
    }










}