using UnityEngine;
using System;

public class Modelado : MonoBehaviour
{
    // EVENTOS
    public event Action OnMemoriaActualizada;
    public event Action<Vector3> OnRuidoPercibido;

    [Header("Referencias")]
    private SensorVision sensor;
    private Oido oido;

    [Header("Creencias (Hechos del Mundo)")]
    public bool sabeRobado = false;
    public bool ladronALaVista = false;
    public bool hayRuidoSinInvestigar = false;
    public bool posicionYaRevisada = false;
    
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

    void OnEnable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto += RegistrarVerLadron;
            sensor.OnLadronPerdido += RegistrarPerderLadron;
            sensor.OnCuadroRobadoDetectado += RegistrarFaltaCuadro;
            sensor.OnCompañeroVisto += RegistrarVerGuardia;
        }

        if (oido != null)
        {
            oido.OnRuidoEscuchado += RelanzarRuidoPercibido;
        }
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto -= RegistrarVerLadron;
            sensor.OnLadronPerdido -= RegistrarPerderLadron;
            sensor.OnCuadroRobadoDetectado -= RegistrarFaltaCuadro;
            sensor.OnCompañeroVisto -= RegistrarVerGuardia;
        }

        if (oido != null)
        {
            oido.OnRuidoEscuchado -= RelanzarRuidoPercibido;
        }
    }

    private void RelanzarRuidoPercibido(Vector3 posicion)
    {
        if (OnRuidoPercibido != null)
            OnRuidoPercibido.Invoke(posicion);
        else
            RegistrarRuido(posicion);
    }

    // Métodos para actualizar la memoria
    public void RegistrarVerLadron(Vector3 posicion, bool llevaElCuadro)
    {
        ladronALaVista = true;
        ultimaPosicionConocidaLadron = posicion;
        posicionYaRevisada = false;
        tiempoUltimoAvistamientoLadron = Time.time;

        if (llevaElCuadro && !sabeRobado)
            sabeRobado = true;

        OnMemoriaActualizada?.Invoke();
    }

    // Actualiza la posición conocida sin tocar ladronALaVista.
    public void ActualizarPosicionConocida(Vector3 posicion, bool llevaElCuadro)
    {
        ultimaPosicionConocidaLadron = posicion;
        posicionYaRevisada = false;
        tiempoUltimoAvistamientoLadron = Time.time;
        if (llevaElCuadro && !sabeRobado) sabeRobado = true;
        OnMemoriaActualizada?.Invoke();
    }

    public void RegistrarPerderLadron()
    {
        ladronALaVista = false;
        OnMemoriaActualizada?.Invoke();
    }

    public void RegistrarFaltaCuadro()
    {
        if (!sabeRobado)
        {
            sabeRobado = true;
            OnMemoriaActualizada?.Invoke();
        }
    }

    // Llamado directamente por CapaComunicacion tras confirmar que el ruido NO era un guardia aliado.
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

    public void MarcarPosicionRevisada()
    {
        posicionYaRevisada = true;
    }
}