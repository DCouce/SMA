using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Runtime.InteropServices;


public class PerdidaVision : MonoBehaviour
{
    private Guardia guardia;
    private SensorVision sensor;

    private NavMeshAgent agent;

    public bool investigandoRuido;
    private Investigar investigar;
    public Transform cuadro;
    public Transform salida;

    private Vector3  ultimaPosicionEscuchada;

    private float tiempoUltimoSonido;
    private float tiempoUltimoSonidoGuardia = 0;

    private NavegacionPatrulla navegacion;


    void Start()
    {
        guardia = GetComponent<Guardia>();
        investigar = GetComponent<Investigar>();
        sensor = GetComponent<SensorVision>();
        agent = GetComponent<NavMeshAgent>();
        navegacion = GetComponent<NavegacionPatrulla>();

    }
    public void ReaccionarAPerdidaDeVision()
    {
        // El guardia vio al ladrón y sabe que LLEVA el cuadro
        if (guardia.robado)
        {
            Debug.Log("Ladrón perdido CON el cuadro. Asegurando la salida.");
            investigar.radio = 3; 
            investigar.puntos = 5;
            agent.SetDestination(salida.position);
            
            if (investigar.puntos_investigacion.Count == 0)
            {
                investigar.GenerateNewPatrolPath(salida.position);
            }
        }
        // El guardia vio al ladrón pero NO LLEVABA el cuadro
        else
        {
            Debug.Log("Ladrón perdido SIN el cuadro. Revisando la zona del botín.");
            investigar.radio = 3;
            investigar.puntos = 5;
            agent.SetDestination(cuadro.position);
            
            if (investigar.puntos_investigacion.Count == 0)
            {
                investigar.GenerateNewPatrolPath(cuadro.position);
            }
        }
    }
}