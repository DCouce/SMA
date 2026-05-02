using UnityEngine;

// Capa de Planificación con arquitectura de subsunción.
//
// Los comportamientos deliberativos se organizan en niveles de prioridad:
//   Nivel 2 (máximo): Revisar   – vimos al ladrón hace poco, vamos a su última posición
//   Nivel 1:          Investigar – hay un ruido sin investigar
//   Nivel 0 (mínimo): Patrullar  – rutina por defecto
//
// Un nivel superior INHIBE a los inferiores.
// Toda la capa opera a PRIORIDAD_PLANIFICACION = 1, por lo que la CapaReactiva
// (prioridad 3) y el BloquearSalida por subasta (prioridad 2) siempre la subsumen.

public class CapaPlanificacion : MonoBehaviour
{
    // ─── Niveles de subsunción internos ──────────────────
    private const int NIVEL_REVISAR    = 2;
    private const int NIVEL_INVESTIGAR = 1;
    private const int NIVEL_PATRULLAR  = 0;

    private int nivelActivo = -1;

    // ─── Referencias ─────────────────────────────────────
    private Modelado           modelo;
    private Control            control;
    private Investigar         investigar;
    private Revisar            revisar;
    private NavegacionPatrulla patrulla;

    void Awake()
    {
        modelo     = GetComponent<Modelado>();
        control    = GetComponent<Control>();
        investigar = GetComponent<Investigar>();
        revisar    = GetComponent<Revisar>();
        patrulla   = GetComponent<NavegacionPatrulla>();
    }

    void OnEnable()
    {
        modelo.OnMemoriaActualizada += EvaluarSituacion;
        control.OnPrioridadLibre    += EvaluarSituacion;
        EvaluarSituacion();
    }

    void OnDisable()
    {
        if (modelo)  modelo.OnMemoriaActualizada -= EvaluarSituacion;
        if (control) control.OnPrioridadLibre    -= EvaluarSituacion;
    }

    //  SUBSUNCIÓN: activar un nivel solo si no hay uno superior activo

    private void Activar(int nivel, MonoBehaviour comportamiento)
    {
        // Si ya es el nivel activo Y el comportamiento está corriendo, nada que hacer.
        // Sin el segundo check, una propuesta rechazada por prioridad superior deja
        // nivelActivo marcado pero el comportamiento sin habilitar, bloqueando el reintento.
        if (nivel == nivelActivo && comportamiento.enabled) return;

        if (nivel != nivelActivo && nivelActivo >= 0)
            DesactivarNivelActual();

        nivelActivo = nivel;
        control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, comportamiento);
    }

    private void DesactivarNivelActual()
    {
        // El Control ya se encarga de deshabilitar el MonoBehaviour anterior
        // al recibir una nueva propuesta de igual o mayor prioridad.
        nivelActivo = -1;
    }

    // ═══════════════════════════════════════════════════════
    //  EVALUACIÓN de la situación (re-evaluada en cada cambio de memoria)
    // ═══════════════════════════════════════════════════════

    private void EvaluarSituacion()
    {
        // La CapaReactiva (PRIORIDAD_REACTIVA = 3) y BloquearSalida por subasta
        // (PRIORIDAD_SUBASTA = 2) siempre tienen prioridad sobre esta capa.
        // Si el ladrón está a la vista, la reactiva ya está actuando: no interferir.
        if (modelo.ladronALaVista) return;

        // Nivel 2: REVISAR – vimos al ladrón hace menos de 5s y no hemos revisado
        if (modelo.TiempoSinVerLadron < 5f && !modelo.posicionYaRevisada)
        {
            revisar.SetDestino(modelo.ultimaPosicionConocidaLadron);
            Activar(NIVEL_REVISAR, revisar);
            return;
        }

        // Nivel 1: INVESTIGAR – hay un ruido pendiente
        if (modelo.hayRuidoSinInvestigar)
        {
            investigar.SetPuntoRuido(modelo.posicionEstimadaRuido);
            Activar(NIVEL_INVESTIGAR, investigar);
            return;
        }

        // Nivel 0: PATRULLAR – rutina por defecto
        Activar(NIVEL_PATRULLAR, patrulla);
    }
}