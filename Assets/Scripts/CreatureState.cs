using UnityEngine;

public class CreatureState : MonoBehaviour
{ 
    public GameObject body;
    public static bool isEvil = true;
    public Material FriendlyMaterial;

    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Pacify")
        {
            isEvil = false;
            body.GetComponent<MeshRenderer>().material = FriendlyMaterial;
        }
    }
}
