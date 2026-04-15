using UnityEngine;

public class Control : MonoBehaviour
{
    private int prioridadActual = -1; 
    private MonoBehaviour accionActual;

    public const int PRIORIDAD_REACTIVA = 2;
    public const int PRIORIDAD_PLANIFICACION = 1;
    public System.Action OnPrioridadLibre; 
    public void RecibirPropuesta(int prioridadPropuesta, MonoBehaviour nuevaAccion)
    {
        if (prioridadPropuesta >= prioridadActual)
        {
            if (accionActual != nuevaAccion)
            {
                if (accionActual != null) accionActual.enabled = false; 
                Debug.Log($"<color=cyan>[CONTROL]</color> Cambio de acción: <b>{nuevaAccion.GetType().Name}</b> (Prioridad: {prioridadPropuesta})");
                prioridadActual = prioridadPropuesta;
                accionActual = nuevaAccion;
                accionActual.enabled = true; 
            }
        }
    }

    public void RetirarPropuesta(int prioridadQueSeRetira)
    {
        if (prioridadActual == prioridadQueSeRetira)
        {
            if (accionActual != null) accionActual.enabled = false;
            prioridadActual = -1;
            accionActual = null;

            OnPrioridadLibre?.Invoke();
        }
    }
}