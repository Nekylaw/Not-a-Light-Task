using UnityEngine;
using UnityEngine.Serialization;

public class ReceptacleManager : MonoBehaviour
{   [SerializeField] Rigidbody orb_Rb; 
    

    float snappingRange = 5f; // allowed range for snapping orb to receptacle
    float snappingForce = 5f; // force that is gonna pull the orb

    void Update()
    {
        SnapToReceptacle();
        UpdateSnapDirection();
    }

    void SnapToReceptacle()
    {
        float Distance = Vector3.Distance(orb_Rb.transform.position, this.transform.position);

        if (Distance < snappingRange) // orb is in the range of the snapping area
        {
            float distanceToSnap = Mathf.InverseLerp(snappingRange, 0f, Distance); 
            float strength = Mathf.Lerp(0f, snappingForce, distanceToSnap); 
            Vector3 directionToReceptacle = (this.transform.position - orb_Rb.transform.position).normalized; 

            orb_Rb.AddForce(directionToReceptacle * strength, ForceMode.Force);// apply force to the orb 

        }
    }

    void UpdateSnapDirection()
    {
        Vector3 receptacleDir = (orb_Rb.transform.position - transform.position).normalized; // direction to the receptacle
        transform.forward = receptacleDir; // make the orb go to that direction 
    }
    
    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("ahh"); 
        if (other.gameObject.tag == "Orb")
        {
            Renderer myRenderer = GetComponent<Renderer>();
            if (myRenderer != null)
            {
                myRenderer.material.color = Color.red;
            } 
            Debug.Log("Orb collided w receptacle !!!!!");
        }
        
    }


        
    
}
