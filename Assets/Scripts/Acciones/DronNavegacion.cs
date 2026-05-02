using UnityEngine;

public class DronNavegacion : MonoBehaviour
{
    [Header("Seguimiento")]
    public Transform objetivo;
    public float alturaVuelo = 4f;
    public float tiempoSuavizado = 0.5f;
    public float velocidadMax = 4f;

    private Vector3 velocidadActual = Vector3.zero;
    void Update()
    {
        if (objetivo == null) return;

        Vector3 destino = new Vector3(
            objetivo.position.x,
            objetivo.position.y + alturaVuelo,
            objetivo.position.z);

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            destino, 
            ref velocidadActual, 
            tiempoSuavizado, 
            velocidadMax
        );
        
        transform.LookAt(objetivo.position);
    }
}
