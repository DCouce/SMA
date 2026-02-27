using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;


public class Oido : MonoBehaviour
{
    private Guardia guardia;
    private SensorVision sensor;

    private NavMeshAgent agent;

    public bool investigandoRuido;
    private Investigar investigar;

    private Vector3  ultimaPosicionEscuchada;

    private float tiempoUltimoSonido;


    void Start()
    {
        guardia = GetComponent<Guardia>();
        investigar = GetComponent<Investigar>();
        sensor = GetComponent<SensorVision>();
        agent = GetComponent<NavMeshAgent>();

    }

    public void OnHeardSound(Vector3 posicionRealDelRuido)
    {
        // Solo investigamos si NO estamos viendo al jugador actualmente
        if (!sensor.DetectarYSeguirConLaMirada())
        {
            Debug.Log("He oído algo por allá...");
            guardia.investigandoRuido = true;
            
            float radioDeIncertidumbre = 2.5f; // Radio dentro del cual el guardia "cree" que está el ruido
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
            investigar.GenerateNewPatrolPath(posicionEstimadaDelRuido);

        }
    }
}