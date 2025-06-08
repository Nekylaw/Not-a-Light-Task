using System.Collections;
using UnityEngine;

public class CreatureStop : MonoBehaviour
{
    [SerializeField] public GameObject creatureOnReceptacle;
    [SerializeField] AnimateLightOrbFeedbackComponent animateLightOrbFeedback;
    [SerializeField] PropLightRendererComponent propLightRendererComponent;
    [SerializeField] private GameObject parent;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Creature") && other.gameObject.GetComponentInChildren<NEW_IAController>().nearestLightObject == animateLightOrbFeedback.gameObject)
        {
            if (animateLightOrbFeedback.isLightOn)
            {
                Debug.Log("crea enter receptacle to eat");
                StartCoroutine(OnEndEatingLight(other.gameObject));
                other.gameObject.GetComponentInChildren<NEW_IAController>().EatLightSource();
                creatureOnReceptacle = other.gameObject;
            }
        }
    }

    public IEnumerator OnEndEatingLight(GameObject creature)
    {
         yield return new WaitForSeconds(5);
         animateLightOrbFeedback.isLightOn = false;
         propLightRendererComponent.gameObject.SetActive(false);
         creature.GetComponent<NEW_IAController>().nearestLightObject = null;
        
    }
    
    
    
}
