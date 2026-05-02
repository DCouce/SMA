using UnityEngine;

// Sensor aéreo del dron. Detecta al ladrón por distancia esférica,
// sin restricción de ángulo y sin comprobación de obstáculos a ras de suelo
// (el dron vuela por encima de las paredes).
// Expone propiedades públicas para que DronComunicacion las consulte periódicamente.
public class DronVision : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetivo;

    [Header("Visión aérea")]
    public float rangoVision = 15f;

    public bool    LadronVisible  { get; private set; }
    public Vector3 PosicionLadron { get; private set; }
    public bool    LadronRobo     { get; private set; }

    private PlayerController player;

    void Start()
    {
        if (objetivo != null)
            player = objetivo.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (objetivo == null) return;

        LadronVisible = Vector3.Distance(transform.position, objetivo.position) <= rangoVision;

        if (LadronVisible)
        {
            PosicionLadron = objetivo.position;
            LadronRobo     = player != null && player.robado;
        }
    }
}
