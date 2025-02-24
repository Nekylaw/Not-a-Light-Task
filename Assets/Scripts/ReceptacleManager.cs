using System;
using UnityEngine;


public class ReceptacleManager : MonoBehaviour
{
    private bool isBright = false;

    private void Update()
    {
        if (isBright)
        {   
            GradualIncreaseBrightness();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Orb")
        {
            other.transform.position = transform.position; 
            UnityEngine.Light lighting = GetComponent<UnityEngine.Light>();
            lighting.enabled = true;
            Destroy(other.attachedRigidbody);
            isBright = true;
        }
        
    }


    private void GradualIncreaseBrightness()
    {  UnityEngine.Light brightness = GetComponent<UnityEngine.Light>();
        if (brightness.intensity <= 20)
        {
            brightness.intensity += 5 * Time.deltaTime; 
        }
        if (brightness.intensity < 100 && brightness.intensity > 20)
        {
           brightness.intensity += 20 * Time.deltaTime; 
        }
        
    }
}
