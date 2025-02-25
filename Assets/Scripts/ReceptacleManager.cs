using Unity.VisualScripting;
using UnityEngine;


public class ReceptacleManager : MonoBehaviour
{
    private bool _isBright;
    public int numReceptacles = 10;
    [SerializeField] string typeOfReceptacle;
    [SerializeField] private Collider orbCollider;
    
    private void Update()
    {
         if (_isBright)
        {   
            PullOrb(orbCollider);
            GradualIncreaseBrightness();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Orb")
        {
            
            //other.transform.position = transform.position; 
            
            Destroy(other.attachedRigidbody);
            Light lighting = GetComponent<Light>();
            lighting.enabled = true;
            _isBright = true;
            if (typeOfReceptacle != "Lampadaire")
            {
                Destroy(other.GameObject());
            }
        }
        
    }

    private void PullOrb(Collider other)
    {
        var step = 10 * Time.deltaTime;
        other.transform.position = Vector3.MoveTowards(other.transform.position, transform.position, step);
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

        numReceptacles -= 1;
    }
    
    
    
}
