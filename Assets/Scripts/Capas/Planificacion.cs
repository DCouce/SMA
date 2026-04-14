using UnityEngine;

public class CapaPlanificacion : MonoBehaviour
{
    private Modelado modelo;
    private Control control;
    
    // Las tres acciones que gestiona esta capa
    private Investigar investigar;
    private Revisar revisar;
    private NavegacionPatrulla patrulla;

    void Awake()
    {
        modelo = GetComponent<Modelado>();
        control = GetComponent<Control>();
        investigar = GetComponent<Investigar>();
        revisar = GetComponent<Revisar>();
        patrulla = GetComponent<NavegacionPatrulla>();
    }

    void OnEnable()
    {
        modelo.OnMemoriaActualizada += EvaluarSituacion;
        EvaluarSituacion(); // Evaluación inicial
    }

    private void EvaluarSituacion()
    {
        // Si hay una emergencia reactiva (ladrón visible), esta capa no interfiere
        if (modelo.ladronALaVista) return;

        // 1. SOSPECHA: Si perdimos al ladrón hace poco, vamos a su última posición
        if (modelo.TiempoSinVerLadron < 5f && modelo.TiempoSinVerLadron > 0)
        {
            revisar.SetDestino(modelo.ultimaPosicionConocidaLadron);
            control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, revisar);
        }
        // 2. CURIOSIDAD: Si hay un ruido pendiente
        else if (modelo.hayRuidoSinInvestigar)
        {
            investigar.SetPuntoRuido(modelo.posicionEstimadaRuido);
            control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, investigar);
        }
        // 3. RUTINA: Si no hay nada, patrullar (Este es el plan por defecto)
        else
        {
            control.RecibirPropuesta(Control.PRIORIDAD_PLANIFICACION, patrulla);
        }
    }

    void OnDisable() { if(modelo) modelo.OnMemoriaActualizada -= EvaluarSituacion; }
}