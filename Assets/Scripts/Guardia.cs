using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class Guardia : MonoBehaviour
{
    // Componentes (Sensores y comportamientos)
    private SensorVision sensor;
    private Oido oido;
    private NavMeshAgent agent;
    
    private Capturar capturar;
    private Perseguir perseguir;
    private Revisar revisar;
    private Investigar investigar;
    private NavegacionPatrulla navegacion;
    private Animator anim;

    public float rangoCaptura = 0.2f;

    [Header("Interfaz")]
    public float tiempoMaximoBusqueda = 5f;

    // Destinos de verificación
    [Header("Comportamiento Post-Persecución")]
    public Transform posicionCuadro; 
    public Transform posicionSalida; 
    [SerializeField] private bool yendoAVerificar = false; // Bandera para saber si está en este estado

    // CAPA DE MODELADO (creencias)
    [Header("Capa de Modelado (Memoria)")]
    [SerializeField] public bool sabeRobado = false; // Si se da cuenta que está robado
    [SerializeField] public bool investigandoRuido = false;
    [SerializeField] private float tiempoSinVerGuardia = 100f; // 
    [SerializeField] private float tiempoSinVerLadron = 100f; // 
    private Vector3 ultimaPosicionConocidaLadron;

    void Start()
    {
        sensor = GetComponent<SensorVision>();
        oido = GetComponent<Oido>();
        agent = GetComponent<NavMeshAgent>();
        capturar = GetComponent<Capturar>();
        perseguir = GetComponent<Perseguir>();
        revisar = GetComponent<Revisar>();
        investigar = GetComponent<Investigar>();
        navegacion = GetComponent<NavegacionPatrulla>();
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
            investigandoRuido = false;
            yendoAVerificar = false; 

            // Captura
            if (sensor.distanciaAlLadron < rangoCaptura)
            {
                capturar.EjecutarCaptura();
                return;
            }
            // Persecución
            else
            {
                perseguir.EjecutarPersecucion(sensor.posicionVeLadron);
                return;
            }
        }

        // PRIORIDAD 2: CAPA DE PLANIFICACIÓN (utilizan base de creencias)
        
        // Cuando lo pierde de vista, revisa ese punto
        else if (tiempoSinVerLadron < tiempoMaximoBusqueda)
        {

            investigandoRuido = false; 
            investigar.puntos_investigacion.Clear();
            revisar.EjecutarRevisar(ultimaPosicionConocidaLadron);

            yendoAVerificar = true;
            return;
        }

        // Cuando lo pierde vista y revisa, se dirige al lugar del cuadro o a la salida a investigar
        else if (yendoAVerificar){
            Transform destinoVerificacion = sabeRobado ? posicionSalida : posicionCuadro;

            agent.destination = destinoVerificacion.position;
            agent.speed = 0.8f;

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                yendoAVerificar = false;
                investigandoRuido = true;
                investigar.GenerateNewPatrolPath(destinoVerificacion.position);
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

        else if (investigandoRuido)
        {

            investigar.Investigacion();
            return;
        }

        // PRIORIDAD 3: RUTINA
        else
        {
            navegacion.Patrullar();
        }
    }

    // La capa de Modelado actualiza la memoria
    private void CapaModelado()
    {
        // Si el sensor ve que el cuadro no está, el guardia "adquiere el conocimiento" (SaberRobado)
        if (sensor.veFaltaCuadro || sensor.veAlLadronConCuadro) sabeRobado = true;

        // Si lo vemos, guardamos su posición
        if (sensor.veAlLadron)
        {
            ultimaPosicionConocidaLadron = sensor.posicionVeLadron;
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

    public void FinalizarInvestigacionRuido() { investigandoRuido = false; }

}