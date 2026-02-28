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
    public bool en_rango_captura = false;
    public bool visto_recientemente = false;
    public int ignorar_ruido = 0;

    public Vector3 puntoDelRuido;

    public float velocidadPatrulla = 0.5f;
    public float velocidadPersecucion = 0.8f;
    public float cooldownIgnorarCompaneros = 0f;

    public GameObject panelDerrota;

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
        // 1. Reducir el cooldown con el tiempo
        if (cooldownIgnorarCompaneros > 0)
        {
            cooldownIgnorarCompaneros -= Time.deltaTime;
            // NUEVO: Si baja de cero, lo clavamos en 0 exactamente
            if (cooldownIgnorarCompaneros <= 0) 
            {
                cooldownIgnorarCompaneros = 0f;
            }
        }

        if (anim != null) 
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }

        // CAPA REACTIVA (basada en sensores directos)
        if (en_rango_captura)
        {
            // Es necesaria la acción "Capturar()"
            panelDerrota.SetActive(true);

        }
        else if (en_vision)
        {
            agent.speed = velocidadPersecucion; 
            navegacion.Perseguir(sensor.objetivo.position);
            agent.updateRotation = false;
            investigandoRuido = false; 
            visto_recientemente = true;
        }
        else if (investigandoRuido)
        {   
            // NUEVO: Comprobar si vemos a un compañero mientras vamos a investigar el ruido
            if (sensor.VerGuardia())
            {
                investigandoRuido = false;
                investigar.puntos_investigacion.Clear(); // Limpiamos la ruta de investigación
                cooldownIgnorarCompaneros = 20f; // Ignora ruidos durante 5 segundos
                navegacion.Patrullar(); // Vuelve a su ruta normal
                return; // Salimos del Update para este frame
            }

            agent.speed = velocidadPatrulla; 
            agent.updateRotation = true;
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                investigar.Investigacion("oido");
            }
        }

        // CAPA PLANIFICACIÓN
        else if (visto_recientemente && !en_vision)
        {
            agent.updateRotation = true;
            if (!agent.pathPending)
            {
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