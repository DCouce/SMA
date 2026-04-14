using UnityEngine;
using System;

public class Modelado : MonoBehaviour
{
    // Evento
    public event Action OnMemoriaActualizada;

    [Header("Referencias")]
    private SensorVision sensor;
    private Oido oido;

    [Header("Creencias (Hechos del Mundo)")]
    public bool sabeRobado = false;
    public bool ladronALaVista = false;
    public bool hayRuidoSinInvestigar = false;
    
    [Header("Ubicaciones Importantes")]
    public Vector3 ultimaPosicionConocidaLadron;
    public Vector3 posicionEstimadaRuido;

    // Timestamps
    private float tiempoUltimoAvistamientoLadron = -100f;
    private float tiempoUltimoAvistamientoGuardia = -100f;

    // Cálculos limpios de tiempo
    public float TiempoSinVerLadron => Time.time - tiempoUltimoAvistamientoLadron;
    public float TiempoSinVerGuardia => Time.time - tiempoUltimoAvistamientoGuardia;

    void Awake()
    {
        sensor = GetComponent<SensorVision>();
        oido = GetComponent<Oido>();
    }

    // Suscripciones
    void OnEnable()
    {
        // Nos conectamos a los sensores para anotar en la memoria
        if (sensor != null)
        {
            sensor.OnLadronVisto += RegistrarVerLadron;
            sensor.OnLadronPerdido += RegistrarPerderLadron;
            sensor.OnCuadroRobadoDetectado += RegistrarFaltaCuadro;
            sensor.OnCompañeroVisto += RegistrarVerGuardia;
        }

        if (oido != null)
        {
            oido.OnRuidoEscuchado += RegistrarRuido;
        }
    }

    void OnDisable()
    {
        // Desconectamos para evitar errores de memoria al cerrar
        if (sensor != null)
        {
            sensor.OnLadronVisto -= RegistrarVerLadron;
            sensor.OnLadronPerdido -= RegistrarPerderLadron;
            sensor.OnCuadroRobadoDetectado -= RegistrarFaltaCuadro;
            sensor.OnCompañeroVisto -= RegistrarVerGuardia;
        }

        if (oido != null)
        {
            oido.OnRuidoEscuchado -= RegistrarRuido;
        }
    }

    // MÉTODOS PARA ACTUALIZAR MEMORIA

    public void RegistrarVerLadron(Vector3 posicion, bool llevaElCuadro)
    {
        ladronALaVista = true;
        ultimaPosicionConocidaLadron = posicion;
        tiempoUltimoAvistamientoLadron = Time.time;

        if (llevaElCuadro && !sabeRobado)
        {
            sabeRobado = true;
        }
        OnMemoriaActualizada?.Invoke(); 
    }

    public void RegistrarPerderLadron()
    {
        ladronALaVista = false;
        OnMemoriaActualizada?.Invoke();
    }

    public void RegistrarFaltaCuadro()
    {
        if (!sabeRobado) // Solo avisamos si es una novedad
        {
            sabeRobado = true;
            OnMemoriaActualizada?.Invoke();
        }
    }

    public void RegistrarRuido(Vector3 posicionRuido)
    {
        posicionEstimadaRuido = posicionRuido;
        hayRuidoSinInvestigar = true;
        OnMemoriaActualizada?.Invoke();
    }

    public void RegistrarVerGuardia()
    {
        tiempoUltimoAvistamientoGuardia = Time.time;
    }


    public void MarcarRuidoComoAtendido()
    {
        hayRuidoSinInvestigar = false;
        OnMemoriaActualizada?.Invoke();
    }
}