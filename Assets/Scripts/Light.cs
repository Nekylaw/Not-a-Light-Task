using UnityEngine;

public class Light : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Orb")
        {
            Debug.Log("ahh");
            Light lighting = GetComponent<Light>();
            lighting.enabled = true;
        }
    }
}
