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
        
        
        
        if (Vector3.Distance(transform.position, cuadro.position)<5)
        {
            // CASO CERCA: Investigación exhaustiva
            investigar.radio = 3; 
            investigar.puntos = 8;
            Debug.Log("Estaba cerca, busco a fondo");
            if(!guardia.robado){
            if(investigar.puntos_investigacion.Count ==0){
            investigar.GenerateNewPatrolPath(transform.position);
            }
            }
            else if(guardia.robado){
            if(investigar.puntos_investigacion.Count ==0){
            investigar.GenerateNewPatrolPath(transform.position);
            }
            }


        }
        else
        {
            // CASO LEJOS: Mirar un poco y rendirse
            investigar.radio = 2;
            investigar.puntos = 4;
            Debug.Log("Estaba lejos, solo echo un vistazo");
            if (!guardia.robado){
            agent.SetDestination(cuadro.position);
            guardia.visto_recientemente = true;
            if(investigar.puntos_investigacion.Count ==0){

            investigar.GenerateNewPatrolPath(cuadro.position);
            }
            }
            else if (guardia.robado){
            agent.SetDestination(salida.position);
            guardia.visto_recientemente = true;
            if(investigar.puntos_investigacion.Count ==0){

            investigar.GenerateNewPatrolPath(salida.position);
            }
            }
        }

        
    
    }
}