using UnityEngine;
using UnityEngine.AI;

public class PerdidaVision : MonoBehaviour
{
    private Guardia guardia;
    private Investigar investigar;
    private NavMeshAgent agent;

    public Transform cuadro;
    public Transform salida;

    void Start()
    {
        guardia = GetComponent<Guardia>();
        investigar = GetComponent<Investigar>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void ReaccionarAPerdidaDeVision()
    {
        // Solo generamos nueva ruta si el guardia no está ya moviéndose a un punto de búsqueda
        if (investigar.puntos_investigacion.Count > 0) return;

        if (guardia.sabeRobado)
        {
            Debug.Log("Ladrón perdido CON el cuadro. Asegurando la salida.");
            investigar.radio = 4; 
            investigar.puntos = 5;
            investigar.GenerateNewPatrolPath(salida.position);
        }
        else
        {
            Debug.Log("Ladrón perdido SIN el cuadro. Revisando el botín.");
            investigar.radio = 3;
            investigar.puntos = 3;
            investigar.GenerateNewPatrolPath(cuadro.position);
        }
    }
}