using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.AI;
using UnityEngine.VFX;

public class CreatureStop : MonoBehaviour
{
    [SerializeField] public GameObject creatureOnReceptacle;
    [SerializeField] AnimateLightOrbFeedbackComponent animateLightOrbFeedback;
    [SerializeField] PropLightRendererComponent propLightRendererComponent;
    [SerializeField] private VisualEffect visualEffect;
    
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
                other.gameObject.GetComponentInChildren<NavMeshAgent>().speed = 0;
            }
        }
    }

    public IEnumerator OnEndEatingLight(GameObject creature)
    {
         yield return new WaitForSeconds(5);
         animateLightOrbFeedback.isLightOn = false;
         propLightRendererComponent.gameObject.SetActive(false);
         creature.GetComponent<NEW_IAController>().nearestLightObject = null;
         creature.gameObject.GetComponentInChildren<NavMeshAgent>().speed = 5;
         visualEffect.gameObject.SetActive(false);
        
    }
    
    
    
}
