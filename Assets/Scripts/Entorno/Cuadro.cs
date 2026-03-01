using UnityEngine;

public class Cuadro : MonoBehaviour
{
    public bool robado = false; 

    public void SerRobado(Transform sitioDondeIr)
    {
        robado = true;

        GetComponent<Collider>().enabled = false;
        if(GetComponent<Rigidbody>()) GetComponent<Rigidbody>().isKinematic = true;

        transform.SetParent(sitioDondeIr);
        transform.localPosition = Vector3.zero; 
        transform.localRotation = Quaternion.identity;
    }
}