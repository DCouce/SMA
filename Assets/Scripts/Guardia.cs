using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class Guardia : MonoBehaviour
{
    private SensorVision sensor;
    private NavegacionPatrulla navegacion;
    private NavMeshAgent agent;
    public bool investigandoRuido;
    public Vector3 puntoDelRuido;
    public float tiempoInvestigacion = 3f;
    private float cronometroInvestigacion;

    void Start()
    {
        sensor = GetComponent<SensorVision>();
        navegacion = GetComponent<NavegacionPatrulla>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
    if (sensor.DetectarYSeguirConLaMirada())
    {

        navegacion.Perseguir(sensor.objetivo.position);
        Debug.Log("Te veo");

        agent.updateRotation = false;
        investigandoRuido = false; // Priorizamos la vista
    }
    else if (investigandoRuido)
        {
            // ESTADO 2: INVESTIGAR RUIDO
            agent.updateRotation = true;
            
            // Si llegamos al punto del ruido, esperamos un poco
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                cronometroInvestigacion -= Time.deltaTime;
                if (cronometroInvestigacion <= 0)
                {
                    investigandoRuido = false;
                }
            }
        }
    else
    {
        agent.updateRotation = true;
        navegacion.Patrullar();
    }
    }
    public void OnHeardSound(Vector3 posicionRealDelRuido)
    {
        // Solo investigamos si NO estamos viendo al jugador actualmente
        if (!sensor.DetectarYSeguirConLaMirada())
        {
            Debug.Log("He oído algo por allá...");
            investigandoRuido = true;
            cronometroInvestigacion = tiempoInvestigacion;
            
            float radioDeIncertidumbre = 5f; // Radio dentro del cual el guardia "cree" que está el ruido
                        
            Vector3 posicionEstimadaDelRuido = posicionRealDelRuido + (Random.insideUnitSphere * radioDeIncertidumbre);
            
            // Debug.DrawLine(posicionRealDelRuido, posicionEstimadaDelRuido, Color.red, 2f); PARA PROBAR
            
            posicionEstimadaDelRuido.y = posicionRealDelRuido.y; 
            agent.SetDestination(posicionEstimadaDelRuido);
        }
    }
  
}