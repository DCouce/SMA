using UnityEngine;

public class GestorZonas : MonoBehaviour
{
    public static GestorZonas Instance { get; private set; }

    public Zona[] zonas;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public Zona ObtenerZona(Vector3 posicion)
    {
        foreach (Zona zona in zonas)
            if (zona.ContienePosicion(posicion)) return zona;
        return null;
    }

    public Zona ObtenerZonaPorNombre(string nombre)
    {
        foreach (Zona zona in zonas)
            if (zona.nombreZona == nombre) return zona;
        return null;
    }
}
