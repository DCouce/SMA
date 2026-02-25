// using UnityEngine;

// public class SensorTorii : MonoBehaviour
// {
//     public AudioSource audioSource;
//     public float rangoRuido = 8f;
//     public LayerMask capaGuardias;

//     private void OnTriggerEnter(Collider other)
//     {
//         // Si pasa alguien (Jugador o Guardia)
//         if (other.CompareTag("Player") || other.gameObject.layer == LayerMask.NameToLayer("Guardias"))
//         {
//             if (audioSource != null) audioSource.Play();
            
//             // REUTILIZAMOS la lógica de tu clase Audio
//             Audio.AlertarGuardiasCercanos(transform.position, rangoRuido, capaGuardias);
//         }
//     }
// }