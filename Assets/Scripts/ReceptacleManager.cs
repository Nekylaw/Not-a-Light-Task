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
        if (other.CompareTag("Orb"))
        {
            other.transform.position = transform.position;
            UnityEngine.Light lighting = GetComponent<UnityEngine.Light>();
            lighting.enabled = true;
            Destroy(other.attachedRigidbody);
            isBright = true;
        }
    }
        private void PullOrb(Collider orb)
    {
        var step = 10 * Time.deltaTime;
        orb.transform.position = Vector3.MoveTowards(orb.transform.position, transform.position, step);
        
    }
    private void GradualIncreaseBrightness()
    {  
        UnityEngine.Light brightness = GetComponent<UnityEngine.Light>();
        if (brightness.intensity <= 200)
        {
            brightness.intensity += 50 * Time.deltaTime; 
        }
        if (brightness.intensity < 1000 && brightness.intensity > 20)
        {
           brightness.intensity += 200 * Time.deltaTime; 
        }
        
    }
}
