using UnityEngine;

// Capa Reactiva con arquitectura de subsunción.
//
// Niveles de prioridad:
//   Nivel 3 (máximo): Capturar       – el ladrón está al alcance
//   Nivel 2:          Perseguir      – el ladrón está a la vista
//   Nivel 1b:         Investigar     – tarea CN: entrar a la zona a buscar al ladrón
//   Nivel 1a:         BloquearSalida – tarea CN: cubrir un punto de entrada/salida
//   Nivel 0 (mínimo): (libre, la planificación decide)
//
// Nivel 1b e 1a comparten el mismo valor de prioridad (1) en Control porque ambos
// vienen de una tarea CN (PRIORIDAD_SUBASTA). El que llegue primero toma el control;
// el CN secuencial garantiza que un guardia solo recibe UNA de las dos.

public class CapaReactiva : MonoBehaviour
{
    private const int NIVEL_CAPTURAR  = 3;
    private const int NIVEL_PERSEGUIR = 2;
    private const int NIVEL_TAREA_CN  = 1; // bloquear o investigar por CN

    private int nivelActivo = -1;

    private SensorVision     sensor;
    private Control          control;
    private Perseguir        perseguir;
    private Capturar         capturar;
    private BloquearSalida   bloquearSalida;
    private Investigar       investigar;
    private CapaComunicacion capaCom;

    void Awake()
    {
        sensor         = GetComponent<SensorVision>();
        control        = GetComponent<Control>();
        perseguir      = GetComponent<Perseguir>();
        capturar       = GetComponent<Capturar>();
        bloquearSalida = GetComponent<BloquearSalida>();
        investigar     = GetComponent<Investigar>();
        capaCom        = GetComponent<CapaComunicacion>();
    }

    void OnEnable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto   += EvaluarLadronVisto;
            sensor.OnLadronPerdido += EvaluarLadronPerdido;
        }

        if (capaCom != null)
        {
            capaCom.OnTareaBloquear   += EvaluarTareaBloquear;
            capaCom.OnTareaInvestigar += EvaluarTareaInvestigar;
            capaCom.OnTareaCancelada  += EvaluarTareaCancelada;
        }
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto   -= EvaluarLadronVisto;
            sensor.OnLadronPerdido -= EvaluarLadronPerdido;
        }

        if (capaCom != null)
        {
            capaCom.OnTareaBloquear   -= EvaluarTareaBloquear;
            capaCom.OnTareaInvestigar -= EvaluarTareaInvestigar;
            capaCom.OnTareaCancelada  -= EvaluarTareaCancelada;
        }
    }

    // ── Subsunción ───────────────────────────────────────────────────────────

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

    // ── Ladrón visible / perdido (Niveles 2 y 3) ─────────────────────────────

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

    // ── Tareas CN (Nivel 1) ───────────────────────────────────────────────────

    // BloquearSalida ya fue configurado por ProcesarComunicacion antes de llegar aquí.
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