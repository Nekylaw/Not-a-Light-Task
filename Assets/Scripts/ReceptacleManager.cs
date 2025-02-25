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
<<<<<<< Updated upstream
    {
        if (other.CompareTag("Orb") )
        {
            other.transform.position = transform.position; 
            UnityEngine.Light lighting = GetComponent<UnityEngine.Light>();
            lighting.enabled = true;
            Destroy(other.attachedRigidbody);
            isBright = true;
=======
    {Debug.Log("ahh");
        if (other.name != "Player")
        {
            //other.transform.position = transform.position; 
            
            Destroy(other.attachedRigidbody);
            Light lighting = GetComponent<Light>();
            lighting.enabled = true;
            _isBright = true;
            if (typeOfReceptacle != "Lampadaire")
            {
                //Destroy(other.GameObject());
            }       
            numReceptacles -= 1;

>>>>>>> Stashed changes
        }
    }

<<<<<<< Updated upstream

=======
    private void PullOrb(Collider orb)
    {
        var step = 10 * Time.deltaTime;
        orb.transform.position = Vector3.MoveTowards(orb.transform.position, transform.position, step);
        
    }
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        
=======

>>>>>>> Stashed changes
    }
}
