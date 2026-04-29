using UnityEngine;

// Capa Reactiva con arquitectura de subsunción.
//
// Los comportamientos se organizan en niveles de prioridad:
//   Nivel 3 (máximo): Capturar   – el ladrón está al alcance
//   Nivel 2:          Perseguir  – el ladrón está a la vista
//   Nivel 1:          BloquearSalida – tarea asignada por Contract Net
//   Nivel 0 (mínimo): (libre, la planificación decide)
//
// Un nivel superior INHIBE (subsume) a todos los inferiores.
// La capa recibe señales de CapaComunicacion (que corre en paralelo)
// y traduce esas señales en activaciones de comportamientos.

public class CapaReactiva : MonoBehaviour
{
    // ─── Niveles de subsunción ───────────────────────────
    private const int NIVEL_CAPTURAR  = 3;
    private const int NIVEL_PERSEGUIR = 2;
    private const int NIVEL_BLOQUEAR  = 1;

    private int nivelActivo = -1;

    // ─── Referencias ─────────────────────────────────────
    private SensorVision      sensor;
    private Control           control;
    private Perseguir         perseguir;
    private Capturar          capturar;
    private BloquearSalida    bloquearSalida;
    private CapaComunicacion  capaCom;

    void Awake()
    {
        sensor         = GetComponent<SensorVision>();
        control        = GetComponent<Control>();
        perseguir      = GetComponent<Perseguir>();
        capturar       = GetComponent<Capturar>();
        bloquearSalida = GetComponent<BloquearSalida>();
        capaCom        = GetComponent<CapaComunicacion>();
    }

    void OnEnable()
    {
        // Escuchamos los eventos del sensor directamente (para reacción inmediata)
        if (sensor != null)
        {
            sensor.OnLadronVisto   += EvaluarLadronVisto;
            sensor.OnLadronPerdido += EvaluarLadronPerdido;
        }

        // Escuchamos eventos de la capa de comunicación (tareas asignadas/canceladas)
        if (capaCom != null)
        {
            capaCom.OnTareaAsignada  += EvaluarTareaAsignada;
            capaCom.OnTareaCancelada += EvaluarTareaCancelada;
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
            capaCom.OnTareaAsignada  -= EvaluarTareaAsignada;
            capaCom.OnTareaCancelada -= EvaluarTareaCancelada;
        }
    }

    // ═══════════════════════════════════════════════════════
    //  SUBSUNCIÓN: activar un nivel solo si no hay uno superior activo
    // ═══════════════════════════════════════════════════════

    private bool Activar(int nivel, MonoBehaviour comportamiento)
    {
        if (nivel < nivelActivo) return false;  // inhibido por nivel superior

        nivelActivo = nivel;
        control.RecibirPropuesta(Control.PRIORIDAD_REACTIVA, comportamiento);
        return true;
    }

    private void Liberar(int nivel)
    {
        if (nivelActivo != nivel) return;  // otro nivel tomó el control
        nivelActivo = -1;
        control.RetirarPropuesta(Control.PRIORIDAD_REACTIVA);
    }

    // ═══════════════════════════════════════════════════════
    //  NIVEL 3 – CAPTURAR (máxima prioridad)
    // ═══════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════
    //  NIVEL 2 – PERSEGUIR
    // ═══════════════════════════════════════════════════════

    // ═══════════════════════════════════════════════════════
    //  EVALUACIÓN al ver/perder ladrón
    // ═══════════════════════════════════════════════════════

    private void EvaluarLadronVisto(Vector3 pos, bool robado)
    {
        float distancia = Vector3.Distance(transform.position, pos);

        // Nivel 3: Capturar si está al alcance
        if (distancia < 0.2f)
        {
            Activar(NIVEL_CAPTURAR, capturar);
            return;
        }

        // Nivel 2: Perseguir (subsume bloqueo si estaba activo)
        perseguir.ActualizarObjetivo(pos);
        Activar(NIVEL_PERSEGUIR, perseguir);
    }

    private void EvaluarLadronPerdido()
    {
        // Liberamos el nivel que teníamos (perseguir o capturar)
        if (nivelActivo == NIVEL_PERSEGUIR || nivelActivo == NIVEL_CAPTURAR)
        {
            Liberar(nivelActivo);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  NIVEL 1 – BLOQUEAR SALIDA (asignada por Contract Net)
    // ═══════════════════════════════════════════════════════

    private void EvaluarTareaAsignada(Vector3 punto, string zona)
    {
        // Solo se activa si no hay un nivel superior (persiguiendo/capturando)
        bloquearSalida.SetPunto(punto, null, null, zona);
        Activar(NIVEL_BLOQUEAR, bloquearSalida);
    }

    private void EvaluarTareaCancelada()
    {
        Liberar(NIVEL_BLOQUEAR);
    }
}