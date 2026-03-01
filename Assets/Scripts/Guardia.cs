using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class Guardia : MonoBehaviour
{
    // Componentes (Sensores y actuadores)
    private SensorVision sensor;
    private Oido oido;
    private NavegacionPatrulla navegacion;
    private NavMeshAgent agent;
    private Investigar investigar;
    private PerdidaVision perdida;
    private Animator anim;

    public float rangoCaptura = 0.2f;
    [Header("Interfaz")]
    public GameObject panelDerrota;
    public float tiempoMaximoBusqueda = 50f;

    // --- CAPA DE MODELADO (creencias) ---
    [Header("Capa de Modelado (Memoria)")]
    [SerializeField] public bool sabeRobado = false; // Si se da cuenta que está robado
    [SerializeField] public bool investigandoRuido = false;
    [SerializeField] public bool visto = false;

    [SerializeField] private float tiempoSinVerGuardia = 0f; // 
    [SerializeField] private float tiempoSinVerLadron = 100f; // 
    private Vector3 ultimaPosicionConocidaLadron;

    void Start()
    {
        sensor = GetComponent<SensorVision>();
        oido = GetComponent<Oido>();
        agent = GetComponent<NavMeshAgent>();
        investigar = GetComponent<Investigar>();
        navegacion = GetComponent<NavegacionPatrulla>();
        perdida = GetComponent<PerdidaVision>();
        anim = GetComponentInChildren<Animator>();
    }

    // CAPA DE CONTROL
    void Update()
    {
        // Actualizar animador (solo parte visual del juego)
        if (anim != null) anim.SetFloat("Speed", agent.velocity.magnitude);

        // CAPA DE MODELADO (Actualiza creencias):
        CapaModelado();

        // PRIORIDAD 1: CAPA REACTIVA (basada en sensores directos)

        if (sensor.veAlLadron)
        {
            investigar.puntos_investigacion.Clear();
            // Captura
            if (sensor.distanciaAlLadron < rangoCaptura)
            {
                EjecutarCaptura();
                return;
            }
            // Persecución
            else
            {
                navegacion.Perseguir(sensor.objetivo.position);
                investigandoRuido = false; 
                return;
            }
        }

        // PRIORIDAD 2: CAPA DE PLANIFICACIÓN (utilizan filtro de creencias)
        
        // Cuando lo pierde de vista, investiga por su zona
        else if (tiempoSinVerLadron < tiempoMaximoBusqueda || visto == true)
        {  
            
            perdida.ReaccionarAPerdidaDeVision();
            
            if (!agent.pathPending  && agent.remainingDistance < 0.5f){
            investigar.Investigacion("vista");
            }
            return;
            
        }

        // Por oido (Nuevo ruido)
        else if (tiempoSinVerGuardia > 10f && oido.escuchadoAlgo)
        {
            investigandoRuido = true;
            investigar.GenerateNewPatrolPath(oido.posicionEstimadaRuido);
            oido.ConsumirRuido(); 
            return;
        }

        // C. Continuar investigación de ruido
        // else if (investigandoRuido)
        // {
        //     investigar.Investigacion("oido");
        //     return;
        // }

        // PRIORIDAD 3: RUTINA
        else
        {
            navegacion.Patrullar();
        }
    }

    // A PARTIR DE AQUÍ VUELVE A ESTAR BIEN
    // La capa de Modelado actualiza la memoria
    private void CapaModelado()
    {
        // Si el sensor ve que el cuadro no está, el guardia "adquiere el conocimiento" (SaberRobado)
        if (sensor.veFaltaCuadro || sensor.veAlLadronConCuadro) sabeRobado = true;

        // Si lo vemos, guardamos su posición
        if (sensor.veAlLadron)
        {
            ultimaPosicionConocidaLadron = sensor.objetivo.position;
            tiempoSinVerLadron = 0f;
        }
        else // Actualizamos tiempo
        {
            tiempoSinVerLadron += Time.deltaTime;
        }

        // Actualizar tiempos sin ver guardia
        if (sensor.veGuardia)
        {
            tiempoSinVerGuardia = 0;
        }
        else
        {
            tiempoSinVerGuardia += Time.deltaTime;
        }
    }

    // REVISAR PARA ELIMINAR
    public void FinalizarBusquedaVisual() { tiempoSinVerLadron = 100f;visto = false;}
    public void FinalizarInvestigacionRuido() { investigandoRuido = false; }

    // ACTUADORES SIMPLES (Por no añadir, scripts)
    private void EjecutarCaptura()
    {
        panelDerrota.SetActive(true);
    }
}