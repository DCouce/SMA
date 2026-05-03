using UnityEngine;

public class DronNavegacion : MonoBehaviour
{
    [Header("Seguimiento")]
    public float alturaVuelo = 1f;
    public float tiempoSuavizado = 0.5f;
    public float velocidadMax = 4f;

    private DronVision vision;
    private Vector3 velocidadActual = Vector3.zero;
    private Vector3 destinoActual;

    void Awake()
    {
        vision = GetComponent<DronVision>();
    }

    void Update()
    {
        if (vision == null) return;

        // Si el dron ve al ladrón, actualiza el destino hacia su posición (con altura)
        if (vision.LadronVisible)
        {
            destinoActual = vision.PosicionLadron + Vector3.up * alturaVuelo;
        }
        // Si no lo ve, se queda donde está (podría quedarse quieto o patrullar lentamente)
        // En este caso, no se mueve; el destinoActual se mantiene en la última posición vista.
        // Opcionalmente, podríamos hacer que se detenga o regrese a una base.

        // Movimiento suave hacia el destino (si no ve al ladrón, destinoActual no se actualiza,
        // por lo que el dron se quedará o se moverá solo si había un destino anterior).
        transform.position = Vector3.SmoothDamp(
            transform.position,
            destinoActual,
            ref velocidadActual,
            tiempoSuavizado,
            velocidadMax
        );

        // Rotación hacia el destino (solo si hay dirección significativa)
        Vector3 dir = destinoActual - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}