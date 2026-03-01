using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class Investigar : MonoBehaviour
{
    public int radio = 2;   
    public int puntos = 3;        

    public List<Vector3> puntos_investigacion = new List<Vector3>();

    private NavMeshAgent agent;
    private Guardia guardia;
    private SensorVision sensor;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        guardia = GetComponent<Guardia>();
        sensor = GetComponent<SensorVision>();
    }

    public void GenerateNewPatrolPath(Vector3 posicion)
    {
        puntos_investigacion.Clear();
        
        for (int i = 0; i < puntos; i++)
        {
            Vector3 randomPoint = GetValidNavMeshPoint(posicion, radio);
            puntos_investigacion.Add(randomPoint);
        }
    }

    private Vector3 GetValidNavMeshPoint(Vector3 center, int range)
    {
        for (int i = 0; i < 10; i++) // Intentar 10 veces si falla el primer punto
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            
            // Comprobar que el punto esté sobre el NavMesh
            if (NavMesh.SamplePosition(randomPoint, out hit, range, NavMesh.AllAreas))
            {
            
                return hit.position;
                
            }
        }
        return center; 
    }

    public void Investigacion(string sentido)
    {
        if (puntos_investigacion.Count == 0) 
        {
            if (sentido == "oido")
            {
                guardia.investigandoRuido = false;
                // NUEVO: Terminé de investigar, no vi a nadie. Me quedo sordo 5 segundos para poder irme de aquí sin que me molesten.
                guardia.cooldownIgnorarCompaneros = 5f; 
            }
            else if (sentido == "vista")
            {
                guardia.visto_recientemente = false;
            }
        }
        
        if (agent.remainingDistance < 0.7f && !agent.pathPending && puntos_investigacion.Count != 0) 
        {
            agent.destination = puntos_investigacion[0];
            puntos_investigacion.RemoveAt(0);
        }
    }

    void OnDrawGizmos()
    {
        if (puntos_investigacion == null) return;
        Gizmos.color = Color.cyan;
        foreach (Vector3 p in puntos_investigacion)
        {
            Gizmos.DrawSphere(p, 0.5f);
        }
    }










}