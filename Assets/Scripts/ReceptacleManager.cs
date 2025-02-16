using UnityEngine;

public class ReceptacleManager : MonoBehaviour
{

        public void OnTriggerEnter(Collider other)
        {
            Debug.Log("ahh");
            if (other.gameObject.tag == "Orb")
            {
                Renderer myRenderer = GetComponent<Renderer>();
                if (myRenderer != null)
                {
                    if (myRenderer.material.color == Color.red)
                    {
                        myRenderer.material.color = Color.green;
                    }
                    else
                    {
                        myRenderer.material.color = Color.red;
                    }
                }
                Debug.Log("Player Collided with CUBE");
            }
            else
            {
                Debug.Log("CUBE trigger called by non-player");
            }
        }
    
}
