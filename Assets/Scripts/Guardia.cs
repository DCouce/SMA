using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class Guardia : MonoBehaviour
{
    private SensorVision sensor;
    private NavegacionPatrulla navegacion;
    private NavMeshAgent agent;
    private Investigar investigar;
    
    private Animator anim;

    public bool investigandoRuido;
    public Vector3 puntoDelRuido;

    public float velocidadPatrulla = 0.5f;
    public float velocidadPersecucion = 1f;

    void Start()
    {
        sensor = GetComponent<SensorVision>();
        navegacion = GetComponent<NavegacionPatrulla>();
        agent = GetComponent<NavMeshAgent>();
        investigar = GetComponent<Investigar>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (anim != null) 
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }

        if (sensor.DetectarYSeguirConLaMirada())
        {
            agent.speed = velocidadPersecucion; 
            
            navegacion.Perseguir(sensor.objetivo.position);
            Debug.Log("Te veo");
            agent.updateRotation = false;
            investigandoRuido = false; 
        }
        else if (investigandoRuido)
        {   
            agent.speed = velocidadPatrulla; 
            
            agent.updateRotation = true;
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                Debug.Log(investigandoRuido);
                investigar.Investigacion();
            }
        }
        else
        {
            agent.speed = velocidadPatrulla; 
            
            agent.updateRotation = true;
            navegacion.Patrullar();
        }
    }
}