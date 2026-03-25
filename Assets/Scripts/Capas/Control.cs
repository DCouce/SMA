using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class Control : MonoBehaviour
{
    private Modelado modelo;
    private Planificacion plan;
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
    public float tiempoMaximoBusqueda = 5f;
    private bool yendoAVerificar = false;

    void Start() {
        modelo = GetComponent<Modelado>();
        plan = GetComponent<Planificacion>();
        sensor = GetComponent<SensorVision>();
        oido = GetComponent<Oido>();
        agent = GetComponent<NavMeshAgent>();
        capturar = GetComponent<Capturar>();
        perseguir = GetComponent<Perseguir>();
        revisar = GetComponent<Revisar>();
        investigar = GetComponent<Investigar>();
        navegacion = GetComponent<NavegacionPatrulla>();
        anim = GetComponentInChildren<Animator>();

        StartCoroutine(CicloDeIA());
    }

    void Update() {
        if (anim != null) anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    IEnumerator CicloDeIA() {
        while (true) {
            ActualizarMemoria();
            // Pedimos la acción a la capa de planificación
            Action accionAEjecutar = plan.DecidirAccion(this, tiempoMaximoBusqueda, yendoAVerificar);
            accionAEjecutar.Invoke();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void ActualizarMemoria() {
        if (sensor.veAlLadron) modelo.RegistrarAvistamiento(sensor.posicionVeLadron);
        if (sensor.veGuardia) modelo.tiempoUltimoAvistamientoGuardia = Time.time;
        if (sensor.veFaltaCuadro || sensor.veAlLadronConCuadro) modelo.sabeRobado = true;
    }

    // ACCIONES LLAMADAS POR PLANIFICACIÓN

    public void AccionPerseguir() {
        LimpiarEstados();
        if (sensor.distanciaAlLadron < rangoCaptura) capturar.EjecutarCaptura();
        else perseguir.EjecutarPersecucion(sensor.posicionVeLadron);
    }

    public void AccionRevisar() {
        LimpiarEstados();
        revisar.EjecutarRevisar(modelo.ultimaPosicionConocidaLadron);
        yendoAVerificar = true;
    }

    public void AccionVerificar() {
        Transform destino = modelo.sabeRobado ? modelo.posicionSalida : modelo.posicionCuadro;
        agent.destination = destino.position;
        agent.speed = 0.8f;

        if (!agent.pathPending && agent.remainingDistance < 0.5f) {
            yendoAVerificar = false;
            modelo.investigandoRuido = true;
            investigar.GenerateNewPatrolPath(destino.position);
        }
    }

    public void AccionNuevoRuido() {
        modelo.investigandoRuido = true;
        investigar.GenerateNewPatrolPath(oido.posicionEstimadaRuido);
        oido.ConsumirRuido();
    }

    public void AccionInvestigarArea() => investigar.Investigacion();

    public void AccionPatrullar() => navegacion.Patrullar();

    public void FinalizarInvestigacionRuido() => modelo.investigandoRuido = false;

    void LimpiarEstados() {
        modelo.investigandoRuido = false;
        yendoAVerificar = false;
    }
}