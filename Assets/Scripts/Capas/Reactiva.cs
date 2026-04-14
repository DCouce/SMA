using UnityEngine;

public class CapaReactiva : MonoBehaviour
{
    private SensorVision sensor;
    private Control control;
    private Perseguir perseguir;
    private Capturar capturar;

    void Awake()
    {
        sensor = GetComponent<SensorVision>();
        control = GetComponent<Control>();
        perseguir = GetComponent<Perseguir>();
        capturar = GetComponent<Capturar>();
    }

    void OnEnable()
    {
        sensor.OnLadronVisto += ReaccionarAlInstante;
        sensor.OnLadronPerdido += PararEmergencia;
    }

    private void ReaccionarAlInstante(Vector3 pos, bool robado)
    {
        float distancia = Vector3.Distance(transform.position, pos);

        if (distancia < 0.2f)
            control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, capturar);
        else
        {
            perseguir.ActualizarObjetivo(pos);
            control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, perseguir);
        }
    }

    private void PararEmergencia() => control.RetirarPropuesta(Control.PRIORIDAD_REACTIVA);

    void OnDisable() { if(sensor) sensor.OnLadronVisto -= ReaccionarAlInstante; }
}