using UnityEngine;

public class OrbManager : MonoBehaviour
{
    public float speed;
	private Rigidbody _rb;
    

    void Update()
    {
        MoveOrbTest();
    }
    
    void MoveOrbTest()
    {
        float moveHorizontal = Input.GetAxis ("Horizontal");
        float moveVertical = Input.GetAxis ("Vertical");
		
        Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVertical);
		
        if( movement != Vector3.zero) {
            GetComponent<Rigidbody>().AddForce(movement * speed * Time.deltaTime);
            
        } 
    }
    
    
    
    

  

    
}
