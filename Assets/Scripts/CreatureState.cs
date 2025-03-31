using UnityEngine;

public class CreatureState : MonoBehaviour
{
    public bool isEvil = true;

    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Pacify")
        {
            isEvil = false;
        }
    }
}
