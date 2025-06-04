using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
public class NEW_IAController : MonoBehaviour


{
    [SerializeField] public GameObject pacifyEffects;

    public GameObject[] allOrbs;
    public GameObject[] allLightSources; 
    public GameObject nearestLightObject;
    public List<GameObject> orbsEaten;
    [SerializeField] private GameObject _orbGameObject;

    float distance;
    float nearestDistance = 100;

    public bool canWander = false;
    public bool isBeingPacified = false;
    public bool isPacified = false;
    public bool canBePet = false;

    private CreatureState creatureState = new CreatureState();
    [SerializeField] private float scaleFactor = 1.5f;

    void LateUpdate()
    {

        if (canWander == true && !isBeingPacified)
        {
            WanderBehaviour();
        }

        if (nearestLightObject != null && isPacified == false && canWander)
        {
            MoveTo(nearestLightObject.transform.position);
        }

        
        if (!isPacified == true && canWander)
        {
            ScanWorldOrbs();
        }

    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("LightSource") && !isPacified)
        {
            other.gameObject.SetActive(false);
            nearestLightObject = null;
            orbsEaten.Add(other.gameObject);
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("LightSource") && !isPacified && other.gameObject.GetComponentInParent<AnimateLightOrbFeedbackComponent>().isLightOn == true)
        {
            other.gameObject.SetActive(false);
            orbsEaten.Add(_orbGameObject.gameObject);
            other.gameObject.GetComponent<AnimateLightOrbFeedbackComponent>().isLightOn = false;
        }
    }

    #region ScanOrbs

    void ScanWorldOrbs()
    {
        allOrbs = GameObject.FindGameObjectsWithTag("Orb");
        allLightSources = GameObject.FindGameObjectsWithTag("LightSource");
        for (int i = 0; i < allOrbs.Length; i++)
        {
            distance = Vector3.Distance(this.transform.position, allOrbs[i].transform.position);
            if (distance < nearestDistance)
            {
                nearestLightObject = allOrbs[i];
                nearestDistance = distance;
                Debug.Log("new nearest orb: " + nearestLightObject.name);
            }
        }
        for (int i = 0; i < allLightSources.Length; i++)
        {
            distance = Vector3.Distance(this.transform.position, allLightSources[i].transform.position);
            if (distance < nearestDistance && allLightSources[i].GetComponentInParent<AnimateLightOrbFeedbackComponent>().isLightOn == true)
            {
                Debug.Log("nearest object is a receptacle light");
                nearestLightObject = allLightSources[i];
                nearestDistance = distance;
            }
        }
    }



    #endregion

    #region Wander

    [SerializeField] float circRadius = 10f;
    [SerializeField] float circDistance = 10f;
    [SerializeField] float wanderRandomizer = 1f;

    Vector3 aiWanderGoal = Vector3.zero;

    public void WanderBehaviour()
    {

        aiWanderGoal += new Vector3(Random.Range(-1.0f, 1.0f) * wanderRandomizer, 0f, Random.Range(-1.0f, 1.0f) * wanderRandomizer);
        aiWanderGoal.Normalize();
        aiWanderGoal *= circRadius;

        var locTarget = aiWanderGoal + new Vector3(0, 0, circDistance);
        var worldCoord = gameObject.transform.InverseTransformVector(locTarget);

        MoveTo(worldCoord);

    }

    void MoveTo(Vector3 location)
    {
        if (isBeingPacified == false)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.SetDestination(location);
        }

    }
    #endregion


    public void StartPacifyEffects()
    {
        if (isBeingPacified == true)
        {
            pacifyEffects.SetActive(true);
        }
    }

    public IEnumerator OnEndPacify()
    {
        yield return new WaitForSeconds(5);
        pacifyEffects.SetActive(false);

        float duration = 5;
        float valueUpY = -5;

        isPacified = true;
        isBeingPacified = false;

        canWander = true;
        foreach (var orb in orbsEaten)
        {
            orb.SetActive(true);
            orb.transform.position = this.transform.position;
            Debug.Log("creature pacified : orb given back !");
        }
        orbsEaten.Clear();
        PacifyBehaviourComponent pacify = new PacifyBehaviourComponent();
        PetManager.Instance.AddCreature(this.gameObject);
    }

    public IEnumerator OnPet()
    {
        if (canBePet)
        {
            canWander = false;
            canBePet = false;
            StartCoroutine(Scale(transform, canBePet));
            yield return new WaitForSeconds(3);
            canWander = true;
        }
    }

    public IEnumerator Scale(Transform creatureBody, bool isPettable)
    {
        Vector3 startScale = creatureBody.localScale;
        Vector3 targetScale = new Vector3();
        float duration = 3f;
        float elapsed = 0f;

        if (isPettable)
        {
            targetScale = startScale * scaleFactor;
        }
        else
        {
            targetScale = startScale / scaleFactor;
        }
        //Debug.Log("targetScale = " + targetScale);

        while (Vector3.Distance(creatureBody.localScale, targetScale) > 0.1f)
        {
            elapsed += Time.deltaTime;
            creatureBody.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        creatureBody.localScale = targetScale;
    }

}
