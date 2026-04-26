using UnityEngine;

public class CapaPlanificacion : MonoBehaviour
{
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

    private void EvaluarSituacion()
    {
        // La capa reactiva (perseguir/capturar) siempre tiene prioridad sobre todo esto.
        // La subasta (PRIORIDAD_SUBASTA = 2) también: si BloquearSalida está activo,
        // RecibirPropuesta(1, patrulla) no lo sobreescribe porque 1 < 2.
        if (modelo.ladronALaVista) return;

        // 1. SOSPECHA: vimos al ladrón hace poco y aún no revisamos esa posición
        if (modelo.TiempoSinVerLadron < 5f && !modelo.posicionYaRevisada)
        {
            revisar.SetDestino(modelo.ultimaPosicionConocidaLadron);
            control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, revisar);
        }
        // 2. RUIDO: hay un sonido pendiente de investigar
        else if (modelo.hayRuidoSinInvestigar)
        {
            investigar.SetPuntoRuido(modelo.posicionEstimadaRuido);
            control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, investigar);
        }
        // 3. RUTINA: patrulla por defecto
        else
        {
            control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, patrulla);
        }
    }

    void OnDisable()
    {
        if (modelo)  modelo.OnMemoriaActualizada -= EvaluarSituacion;
        if (control) control.OnPrioridadLibre    -= EvaluarSituacion;
    }
}
