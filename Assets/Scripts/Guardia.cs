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
    
  
}