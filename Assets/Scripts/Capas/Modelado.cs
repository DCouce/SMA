using UnityEngine;
using System;

public class Modelado : MonoBehaviour
{
    // ── Eventos ──────────────────────────────────────────────────────────────

    // Disparado cuando cambia cualquier creencia (planificación lo escucha)
    public event Action OnMemoriaActualizada;

    // [NUEVO] Disparado cuando el Oido percibe un ruido, ANTES de registrarlo.
    // CapaComunicacion lo intercepta para enviar un QueryIf y decidir si el
    // ruido merece ser registrado (puede ser un guardia aliado andando).
    // Solo si nadie confirma en el timeout, CapaComunicacion llama a
    // RegistrarRuido() directamente.
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
    private float tiempoUltimoAvistamientoLadron  = -100f;
    private float tiempoUltimoAvistamientoGuardia = -100f;

    // Cálculos limpios de tiempo
    public float TiempoSinVerLadron  => Time.time - tiempoUltimoAvistamientoLadron;
    public float TiempoSinVerGuardia => Time.time - tiempoUltimoAvistamientoGuardia;

    void Awake()
    {
        sensor = GetComponent<SensorVision>();
        oido   = GetComponent<Oido>();
    }

    void OnEnable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto          += RegistrarVerLadron;
            sensor.OnLadronPerdido        += RegistrarPerderLadron;
            sensor.OnCuadroRobadoDetectado += RegistrarFaltaCuadro;
            sensor.OnCompañeroVisto       += RegistrarVerGuardia;
        }

        if (oido != null)
        {
            // [MODIFICADO] Ahora redirigimos el ruido a través de OnRuidoPercibido
            // en lugar de registrarlo directamente. CapaComunicacion decide si
            // finalmente llama a RegistrarRuido().
            oido.OnRuidoEscuchado += RelanzarRuidoPercibido;
        }
    }

    void OnDisable()
    {
        if (sensor != null)
        {
            sensor.OnLadronVisto          -= RegistrarVerLadron;
            sensor.OnLadronPerdido        -= RegistrarPerderLadron;
            sensor.OnCuadroRobadoDetectado -= RegistrarFaltaCuadro;
            sensor.OnCompañeroVisto       -= RegistrarVerGuardia;
        }

        if (oido != null)
        {
            oido.OnRuidoEscuchado -= RelanzarRuidoPercibido;
        }
    }

    // ── Intermediario: re-emite el ruido como OnRuidoPercibido ───────────────
    // CapaComunicacion escucha este evento. Si no hay nadie suscrito (caso poco
    // probable), registramos el ruido directamente como fallback de seguridad.
    private void RelanzarRuidoPercibido(Vector3 posicion)
    {
        if (OnRuidoPercibido != null)
            OnRuidoPercibido.Invoke(posicion);
        else
            RegistrarRuido(posicion); // fallback: sin capa de comunicación activa
    }

    // ── Métodos para actualizar la memoria ───────────────────────────────────

    public void RegistrarVerLadron(Vector3 posicion, bool llevaElCuadro)
    {
        ladronALaVista               = true;
        ultimaPosicionConocidaLadron = posicion;
        posicionYaRevisada           = false;
        tiempoUltimoAvistamientoLadron = Time.time;

        if (llevaElCuadro && !sabeRobado)
            sabeRobado = true;

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

    // Llamado directamente por CapaComunicacion tras confirmar que el ruido
    // NO era un guardia aliado.
    public void RegistrarRuido(Vector3 posicionRuido)
    {
        posicionEstimadaRuido  = posicionRuido;
        hayRuidoSinInvestigar  = true;
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
        // Sin evento: RetirarPropuesta disparará OnPrioridadLibre y re-evaluará
    }
}