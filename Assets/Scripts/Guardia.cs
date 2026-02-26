using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class Guardia : MonoBehaviour
{
    private SensorVision sensor;
    private NavegacionPatrulla navegacion;
    private NavMeshAgent agent;
    private Investigar investigar;

    public bool investigandoRuido;
    public Vector3 puntoDelRuido;

    void Start()
    {
        sensor = GetComponent<SensorVision>();
        navegacion = GetComponent<NavegacionPatrulla>();
        agent = GetComponent<NavMeshAgent>();
        investigar = GetComponent<Investigar>();

    }

    void Update()
    {
    if (sensor.DetectarYSeguirConLaMirada())
    {

        navegacion.Perseguir(sensor.objetivo.position);
        Debug.Log("Te veo");

        agent.updateRotation = false;
        investigandoRuido = false; // Priorizamos la vista
    }
    else if (investigandoRuido)
        {   

            // ESTADO 2: INVESTIGAR RUIDO
            agent.updateRotation = true;
            
            if (!agent.pathPending && agent.remainingDistance < 0.5f)

            {
                Debug.Log(investigandoRuido);
                investigar.Investigacion();
                
            }


            
        }
    else
    {
        agent.updateRotation = true;
        navegacion.Patrullar();
    }
    }
    public void OnHeardSound(Vector3 posicionRealDelRuido)
    {
        // Solo investigamos si NO estamos viendo al jugador actualmente
        if (!sensor.DetectarYSeguirConLaMirada())
        {
            Debug.Log("He oído algo por allá...");
            investigandoRuido = true;
            
            float radioDeIncertidumbre = 5f; // Radio dentro del cual el guardia "cree" que está el ruido
                        
            Vector3 posicionEstimadaDelRuido = posicionRealDelRuido + (Random.insideUnitSphere * radioDeIncertidumbre);
            
            // Debug.DrawLine(posicionRealDelRuido, posicionEstimadaDelRuido, Color.red, 2f); PARA PROBAR
            
            posicionEstimadaDelRuido.y = posicionRealDelRuido.y; 
            agent.SetDestination(posicionEstimadaDelRuido);
            investigar.GenerateNewPatrolPath(posicionEstimadaDelRuido);

        }
    }
  
}