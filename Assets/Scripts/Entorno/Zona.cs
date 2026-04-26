using UnityEngine;

// Representa una zona del mapa con sus puntos de entrada/salida.
// Añadir un BoxCollider (no-trigger) al mismo GameObject: se usa para Contains().
// Los puntosEntrada son Transforms vacíos colocados a mano en las puertas del escenario.
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
}
