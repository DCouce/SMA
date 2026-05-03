using UnityEngine;

// Capa Reactiva con arquitectura de subsunción.
public class CapaReactiva : MonoBehaviour
{
    private const int NIVEL_CAPTURAR = 3;
    private const int NIVEL_PERSEGUIR = 2;
    private const int NIVEL_TAREA_CN = 1;

    private int nivelActivo = -1;

    private SensorVision sensor;
    private Control control;
    private Perseguir perseguir;
    private Capturar capturar;
    private BloquearSalida bloquearSalida;
    private Investigar investigar;
    private CapaComunicacion capaCom;

    void Awake()
    {
        sensor = GetComponent<SensorVision>();
        control = GetComponent<Control>();
        perseguir = GetComponent<Perseguir>();
        capturar = GetComponent<Capturar>();
        bloquearSalida = GetComponent<BloquearSalida>();
        investigar = GetComponent<Investigar>();
        capaCom = GetComponent<CapaComunicacion>();
    }

    void OnEnable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto += EvaluarLadronVisto;
            sensor.OnLadronPerdido += EvaluarLadronPerdido;
        }

        if (capaCom != null)
        {
            capaCom.OnTareaBloquear += EvaluarTareaBloquear;
            capaCom.OnTareaInvestigar += EvaluarTareaInvestigar;
            capaCom.OnTareaCancelada += EvaluarTareaCancelada;
        }
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto -= EvaluarLadronVisto;
            sensor.OnLadronPerdido -= EvaluarLadronPerdido;
        }

        if (capaCom != null)
        {
            capaCom.OnTareaBloquear -= EvaluarTareaBloquear;
            capaCom.OnTareaInvestigar -= EvaluarTareaInvestigar;
            capaCom.OnTareaCancelada -= EvaluarTareaCancelada;
        }
    }

    // SUBSUNCIÓN
    private bool Activar(int nivel, MonoBehaviour comportamiento)
    {
        if (nivel < nivelActivo) return false;
        nivelActivo = nivel;
        control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, comportamiento);
        return true;
    }

    private void Liberar(int nivel)
    {
        if (nivelActivo != nivel) return;
        nivelActivo = -1;
        control.RetirarPropuesta(Control.PRIORIDAD_REACTIVA);
    }

    // Ladrón visible o perdido (Niveles 2 y 3)
    private void EvaluarLadronVisto(Vector3 pos, bool robado)
    {
        float distancia = Vector3.Distance(transform.position, pos);

        if (distancia < 0.2f)
        {
            Activar(NIVEL_CAPTURAR, capturar);
            return;
        }

        perseguir.ActualizarObjetivo(pos);
        Activar(NIVEL_PERSEGUIR, perseguir);
    }

    private void EvaluarLadronPerdido()
    {
        if (nivelActivo == NIVEL_PERSEGUIR || nivelActivo == NIVEL_CAPTURAR)
            Liberar(nivelActivo);
    }

    // Tareas CN (Nivel 1) 
    private void EvaluarTareaBloquear(Vector3 punto, string zona)
    {
        Activar(NIVEL_TAREA_CN, bloquearSalida);
    }

    // Investigar ya fue configurado con SetPuntoRuido por ProcesarComunicacion.
    private void EvaluarTareaInvestigar(Vector3 punto, string zona)
    {
        Activar(NIVEL_TAREA_CN, investigar);
    }

    private void EvaluarTareaCancelada()
    {
        Liberar(NIVEL_TAREA_CN);
    }
}