using UnityEngine;

public class SensorVision : MonoBehaviour
{
    public Transform objetivo;
    public Transform[] guardias;
    public float rangoCaptura = 0.2f;
    public float rangoVision = 15f;
    public float anguloVision = 45f;
    public float velocidadGiroManual = 5f;
    private Guardia guardia;
    private PlayerController player;

    // Visión del cuadro
    public Transform posicionBaseCuadro;
    public Transform cuadroFisico;


    void Start()
    {
        guardia = GetComponent<Guardia>();
        player = objetivo.GetComponent<PlayerController>();

    }
    void Update()
    {
        // Se actualiza la visión según un booleano
        guardia.en_vision = DetectarYSeguirConLaMirada();

        // Solo estará en rango de captura si lo estamos viendo Y además está cerca
        guardia.en_rango_captura = guardia.en_vision && EnRangoDeCaptura(rangoCaptura);

        if (!guardia.robado && VerFaltaCuadro()) // Por eficiencia, solo lo revisará si aún no sabe que fue robado
        {
            Debug.Log("Falta el Cuadro");
            guardia.robado = true;
        }
    }

    public bool VerFaltaCuadro()
    {
        return (Vector3.Distance(cuadroFisico.position, posicionBaseCuadro.position) > 0.1f) && EstaEnConoDeVision(posicionBaseCuadro, true);
    }

    public bool DetectarYSeguirConLaMirada()
    {
        if (objetivo == null) return false;

        // 1. Intentamos ver si está en el cono actual
        bool enCono = EstaEnConoDeVision(objetivo);

        // 2. Si lo vemos (o si estaba justo en el borde), giramos hacia él
        // Esto hace que el "cono" se mueva, permitiendo que la persecución continúe
        if (enCono)
        {
            GirarSuavementeHacia(objetivo.position);
            guardia.robado = player.robado;
        }

        return enCono;
    }
    public bool VerGuardia()
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

    public bool EnRangoDeCaptura(float rangoCaptura)
    {
        return Vector3.Distance(transform.position, objetivo.position) <= rangoCaptura;
    }

    // REVISAR, SOLO PARA DEBUGGING: Dibuja el cono de visión en la escena
    private void OnDrawGizmos()
{
    // 1. Dibujar el rango de distancia (Círculo amarillo)
    Gizmos.color = Color.yellow;
    // Dibujamos una esfera de cables para ver el área total de alcance
    Gizmos.DrawWireSphere(transform.position, rangoVision);

    // 2. Calcular las líneas que forman el ángulo del cono
    // Usamos el forward del objeto y lo rotamos hacia la izquierda y derecha
    Vector3 lineaDerecha = Quaternion.AngleAxis(anguloVision, transform.up) * transform.forward;
    Vector3 lineaIzquierda = Quaternion.AngleAxis(-anguloVision, transform.up) * transform.forward;

    // 3. Dibujar las líneas del cono (Rojo para el ángulo de visión)
    Gizmos.color = Color.red;
    Gizmos.DrawRay(transform.position, lineaDerecha * rangoVision);
    Gizmos.DrawRay(transform.position, lineaIzquierda * rangoVision);

    // 4. Línea opcional hacia el objetivo si está a la vista
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