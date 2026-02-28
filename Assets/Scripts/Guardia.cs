using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class Guardia : MonoBehaviour
{
    private SensorVision sensor;
    private NavegacionPatrulla navegacion;
    private NavMeshAgent agent;
    private Investigar investigar;
    private PerdidaVision perdida;
    
    private Animator anim;

    public bool investigandoRuido;
    public bool robado = false;
    public bool en_vision = false;
    public bool visto_recientemente = false;
    public int ignorar_ruido = 0;

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
        perdida = GetComponent<PerdidaVision>();
    }

    void Update()
    {
        if (anim != null) 
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }

        if (en_vision)
        {
            agent.speed = velocidadPersecucion; 
            
            navegacion.Perseguir(sensor.objetivo.position);
            Debug.Log("Te veo");
            agent.updateRotation = false;
            investigandoRuido = false; 
            visto_recientemente = true;
        }
       
        else if (investigandoRuido)
        {   
            
            agent.speed = velocidadPatrulla; 
            
            agent.updateRotation = true;
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                Debug.Log(investigandoRuido);
                investigar.Investigacion("oido");
            }
            
        }
        else if (visto_recientemente && !en_vision)
        {
            agent.updateRotation = true;
            Debug.Log("Messi");


            if (!agent.pathPending ){
            perdida.ReaccionarAPerdidaDeVision();
            investigar.Investigacion("vista");
            }
        }
        else
        {
            visto_recientemente = false;
            agent.speed = velocidadPatrulla; 
            
            agent.updateRotation = true;
            navegacion.Patrullar();
        }
    }
}