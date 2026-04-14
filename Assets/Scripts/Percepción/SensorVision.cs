using UnityEngine;
using System;

public class SensorVision : MonoBehaviour
{
    [Header("Referencias del Entorno")]
    public Transform objetivo; // El objeto del ladrón
    private PlayerController player;
    public Transform[] guardias; // Otros guardias (para evitar confundir)
    public Transform posicionBaseCuadro; // El atril
    public Transform cuadroFisico; // El cuadro a robar

    [Header("Configuración de Visión")]
    public float rangoVision = 5f;
    public float anguloVision = 45f;
    public float velocidadGiroManual = 5f;
    
    private Modelado modelo;
    private bool veiaAlLadronAntes = false;
    
    // EVENTOS
    public event Action<Vector3, bool> OnLadronVisto;
    public event Action OnLadronPerdido;
    public event Action OnCuadroRobadoDetectado;
    public event Action OnCompañeroVisto;

    void Start()
    {
        // Vinculamos el script del jugador para poder conocer su estado
        if (objetivo != null)
            {
                player = objetivo.GetComponent<PlayerController>();
            }
    }
    void Update()
    {
        // GESTIÓN DEL LADRÓN
        bool veAlLadronAhora = EstaEnConoDeVision(objetivo);

        if (veAlLadronAhora)
        {
            OnLadronVisto?.Invoke(objetivo.position, player.robado);
            veiaAlLadronAntes = true;
            GirarSuavementeHacia(objetivo.position);
        }

        else if (veiaAlLadronAntes && !veAlLadronAhora)
        {
            OnLadronPerdido?.Invoke();
            veiaAlLadronAntes = false;
        }

        // GESTIÓN DEL CUADRO
        if (VeFaltaCuadro())
        {
            OnCuadroRobadoDetectado?.Invoke();
        }

        // GESTIÓN DE COMPAÑEROS
        if (VeGuardia())
        {
        OnCompañeroVisto?.Invoke();
        }
    }

    private bool VeFaltaCuadro()
    {
        float distanciaAlAtril = Vector3.Distance(cuadroFisico.position, posicionBaseCuadro.position);
        return (distanciaAlAtril > 0.1f) && EstaEnConoDeVision(posicionBaseCuadro, true);
    }

    public bool VeGuardia()
    {
        foreach (Transform obj in guardias)
        {
            if (EstaEnConoDeVision(obj))
            {
                return true;
            }

        }  
        return false;
        
    }
    private bool EstaEnConoDeVision(Transform objetivo, bool esPuntoVacio = false)
    {
        float distancia = Vector3.Distance(transform.position, objetivo.position);
        if (distancia > rangoVision) return false;

        Vector3 direccionAlObjetivo = (objetivo.position - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, direccionAlObjetivo);

        if (angulo < anguloVision)
        {
            RaycastHit hit;
            // Raycast para evitar ver a través de paredes
            if (Physics.Raycast(transform.position + Vector3.up * 0.05f, direccionAlObjetivo, out hit, rangoVision, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (esPuntoVacio)
                {
                    return hit.distance >= (distancia - 0.05f);
                }
                else
                {   // Para el ladrón, debe chocar con él
                    return hit.transform == objetivo;
                }
            }
            // No choca con nada, también lo ve si era punto vacío
            return esPuntoVacio;
        }
        return false;
    }

    private void GirarSuavementeHacia(Vector3 destino)
    {
        Vector3 direccion = (destino - transform.position).normalized;
        direccion.y = 0;
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * velocidadGiroManual);
    }


    // REVISAR, SOLO PARA DEBUGGING: Dibuja el cono de visión en la escena
    private void OnDrawGizmos()
    {
    // Dibujar el rango de distancia (Círculo amarillo)
    Gizmos.color = Color.yellow;
    // Dibujamos una esfera de cables para ver el área total de alcance
    Gizmos.DrawWireSphere(transform.position, rangoVision);

    // Calcular las líneas que forman el ángulo del cono
    // Usamos el forward del objeto y lo rotamos hacia la izquierda y derecha
    Vector3 lineaDerecha = Quaternion.AngleAxis(anguloVision, transform.up) * transform.forward;
    Vector3 lineaIzquierda = Quaternion.AngleAxis(-anguloVision, transform.up) * transform.forward;

    // Dibujar las líneas del cono (Rojo para el ángulo de visión)
    Gizmos.color = Color.red;
    Gizmos.DrawRay(transform.position, lineaDerecha * rangoVision);
    Gizmos.DrawRay(transform.position, lineaIzquierda * rangoVision);

    // Línea opcional hacia el objetivo si está a la vista
    if (objetivo != null)
    {
        float distancia = Vector3.Distance(transform.position, objetivo.position);
        if (distancia < rangoVision)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, objetivo.position);
        }
    }
    }
}