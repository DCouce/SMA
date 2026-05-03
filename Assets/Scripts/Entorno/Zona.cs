using UnityEngine;

// Representa una zona del mapa con sus puntos de entrada/salida/estratégicos.
public class Zona : MonoBehaviour
{
    [Header("Identificación")]
    public string nombreZona;

    [Header("Puntos de Entrada/Salida")]
    public Transform[] puntosEntrada;
    private BoxCollider[] colliders;

    void Awake() => colliders = GetComponentsInChildren<BoxCollider>();

    public bool ContienePosicion(Vector3 pos)
    {
        foreach (var col in colliders)
        {
            if (col.bounds.Contains(pos)) return true;
        }
        return false;
    }

    void OnDrawGizmos()
{
    Gizmos.color = nombreZona switch
    {
        "Zona1" => new Color(1f, 0f, 0f, 0.2f),    // rojo
        "Zona2" => new Color(0f, 1f, 0f, 0.2f),    // verde
        "Zona3" => new Color(0f, 0f, 1f, 0.2f),    // azul
        "Zona4" => new Color(1f, 1f, 0f, 0.2f),    // amarillo
        "Zona5" => new Color(1f, 0f, 1f, 0.2f),    // magenta
        _       => new Color(1f, 1f, 1f, 0.2f),    // blanco
    };

    foreach (BoxCollider col in GetComponentsInChildren<BoxCollider>())
    {
        Gizmos.matrix = col.transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
    }
}
}
