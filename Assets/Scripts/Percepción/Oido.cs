using UnityEngine;
using System;

public class Oido : MonoBehaviour
{
    [Header("Configuración Auditiva")]
    public float radioDeIncertidumbre = 2.5f;

    public event Action<Vector3> OnRuidoEscuchado;

    // Variables internas para la lógica de precisión
    private Vector3 ultimaPosicionReal;
    private float tiempoUltimoSonido;

    public void OnHeardSound(Vector3 posicionRealDelRuido)
    {   
        // Por defecto usamos el radio normal
        float radioAAplicar = radioDeIncertidumbre;

        // Lógica de precisión : Si ya escuchamos algo antes
        if (ultimaPosicionReal != Vector3.zero)
        {
            // Si el ruido actual está a menos de 1m del anterior Y fue hace menos de 2 segundos
            if (Vector3.Distance(posicionRealDelRuido, ultimaPosicionReal) < 1f && (Time.time - tiempoUltimoSonido) < 2f)
            {
                radioAAplicar = 1f; // Se vuelve más preciso (radio 1.0)
            }
        }

        // Calculamos la posición con error (no debe ser exacto)
        Vector3 calculoConError = posicionRealDelRuido + (UnityEngine.Random.insideUnitSphere * radioAAplicar);        
        calculoConError.y = posicionRealDelRuido.y; 

        ultimaPosicionReal = posicionRealDelRuido;
        tiempoUltimoSonido = Time.time;

        // Guardamos los datos para la siguiente comparación
        ultimaPosicionReal = posicionRealDelRuido;
        tiempoUltimoSonido = Time.time;  

        OnRuidoEscuchado?.Invoke(calculoConError);  
    }
}