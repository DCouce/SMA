using UnityEngine;

public class SensorVision : MonoBehaviour
{
    public Transform objetivo;
    public float rangoVision = 15f;
    public float anguloVision = 45f;
    public float velocidadGiroManual = 5f;

    public bool DetectarYSeguirConLaMirada()
    {
        if (objetivo == null) return false;

        // 1. Intentamos ver si está en el cono actual
        bool enCono = EstaEnConoDeVision();

        // 2. Si lo vemos (o si estaba justo en el borde), giramos hacia él
        // Esto hace que el "cono" se mueva, permitiendo que la persecución continúe
        if (enCono)
        {
            GirarSuavementeHacia(objetivo.position);
        }

        return enCono;
    }

    private bool EstaEnConoDeVision()
    {
        float distancia = Vector3.Distance(transform.position, objetivo.position);
        if (distancia > rangoVision) return false;

        Vector3 direccionAlObjetivo = (objetivo.position - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, direccionAlObjetivo);

        if (angulo < anguloVision)
        {
            RaycastHit hit;
            // Raycast para evitar ver a través de paredes
            if (Physics.Raycast(transform.position + Vector3.up * 0.05f, direccionAlObjetivo, out hit, rangoVision))
            {
                return hit.transform == objetivo;
            }
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
}