using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Net;
using System.Runtime.InteropServices;


public class Oido : MonoBehaviour
{
    private Guardia guardia;
    private SensorVision sensor;

    private NavMeshAgent agent;

    public bool investigandoRuido;
    private Investigar investigar;

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

    public void OnHeardSound(Vector3 posicionRealDelRuido)
    {   
        // NUEVO: Si estamos en cooldown por haber visto a un colega, somos sordos temporalmente
        if (guardia.cooldownIgnorarCompaneros > 0) 
        {
            return; 
        }
        
        // Solo investigamos si NO estamos viendo al jugador actualmente
        if(Time.time - tiempoUltimoSonidoGuardia >5){
        if (!sensor.DetectarYSeguirConLaMirada() )
        {
            Debug.Log("He oído algo por allá...");
            
            float radioDeIncertidumbre = 2.5f; // Radio dentro del cual el guardia "cree" que está el ruido
            guardia.investigandoRuido = true;
            if (ultimaPosicionEscuchada != new Vector3())
            {
                if (Vector3.Distance(posicionRealDelRuido, ultimaPosicionEscuchada) < 1 && (Time.time - tiempoUltimoSonido) < 2)
                {
                    radioDeIncertidumbre = 1f;
                }

            }
            ultimaPosicionEscuchada = posicionRealDelRuido;
            tiempoUltimoSonido = Time.time;
                        
            Vector3 posicionEstimadaDelRuido = posicionRealDelRuido + (Random.insideUnitSphere * radioDeIncertidumbre);
            
            // Debug.DrawLine(posicionRealDelRuido, posicionEstimadaDelRuido, Color.red, 2f); PARA PROBAR
            
            posicionEstimadaDelRuido.y = posicionRealDelRuido.y; 
            agent.SetDestination(posicionEstimadaDelRuido);
            if (sensor.VerGuardia())
            {
                Debug.Log("Guardia");
                tiempoUltimoSonidoGuardia = Time.time;
            }
            

            investigar.GenerateNewPatrolPath(posicionEstimadaDelRuido);
        
        }
            

        }
        else
            {
                investigar.puntos_investigacion.Clear();
                guardia.investigandoRuido = false;
                navegacion.Patrullar();

            }
    }
}