using UnityEngine;
using System;

public class Planificacion : MonoBehaviour
{
    private Modelado modelo;
    private SensorVision sensor;
    private Oido oido;

    void Awake() {
        modelo = GetComponent<Modelado>();
        sensor = GetComponent<SensorVision>();
        oido = GetComponent<Oido>();
    }

    // Esta función devuelve una accion, una referencia a un método
    public Action DecidirAccion(Control control, float tiempoMaxBusqueda, bool yendoAVerificar) 
    {
        // 1. Prioridad Reactiva
        if (sensor.veAlLadron) return control.AccionPerseguir;

        // 2. Prioridad de Planificación
        if (modelo.TiempoSinVerLadron < tiempoMaxBusqueda) return control.AccionRevisar;
        
        if (yendoAVerificar) return control.AccionVerificar;

        if (modelo.TiempoSinVerGuardia > 10f && oido.escuchadoAlgo) return control.AccionNuevoRuido;

        if (modelo.investigandoRuido) return control.AccionInvestigarArea;

        // 3. Rutina
        return control.AccionPatrullar;
    }
}