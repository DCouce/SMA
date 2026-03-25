using UnityEngine;

public class Modelado : MonoBehaviour
{
    public bool sabeRobado = false;
    public bool investigandoRuido = false;
    public Vector3 ultimaPosicionConocidaLadron;
    public float tiempoUltimoAvistamientoLadron = -100f;
    public float tiempoUltimoAvistamientoGuardia = -100f;

    public Transform posicionCuadro; 
    public Transform posicionSalida; 

    public void RegistrarAvistamiento(Vector3 pos) {
        ultimaPosicionConocidaLadron = pos;
        tiempoUltimoAvistamientoLadron = Time.time;
    }

    public float TiempoSinVerLadron => Time.time - tiempoUltimoAvistamientoLadron;
    public float TiempoSinVerGuardia => Time.time - tiempoUltimoAvistamientoGuardia;
}