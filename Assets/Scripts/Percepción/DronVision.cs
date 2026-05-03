using UnityEngine;

public class DronVision : MonoBehaviour
{
    [Header("Referencias")]
    public Transform objetivo;

    [Header("Visión aérea")]
    public float rangoVision = 5f;

    public bool LadronVisible { get; private set; }
    public Vector3 PosicionLadron { get; private set; }
    public bool LadronRobo { get; private set; }

    private PlayerController player;

    void Start()
    {
        if (objetivo != null)
            player = objetivo.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (objetivo == null) return;

        // Detección por distancia esférica
        LadronVisible = Vector3.Distance(transform.position, objetivo.position) <= rangoVision;

        if (LadronVisible)
        {
            PosicionLadron = objetivo.position;
            LadronRobo = player != null && player.robado;
        }
    }
}
