using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;
using DG.Tweening;
using Game.Services.LightSources;

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
    [SerializeField] private GameObject newOrb;

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
        if (other.gameObject.layer == 10 && !isPacified)
        {
            other.gameObject.SetActive(false);
            nearestLightObject = null;
            orbsEaten.Add(other.gameObject);
        }
    }
    
    public void EatLightSource()
    {
        Debug.Log("creature eating light");
        var lightParticules = nearestLightObject.GetComponentInChildren<VisualEffect>().gameObject;
        lightParticules.transform.DOMove(this.gameObject.transform.position, 5);
        orbsEaten.Add(_orbGameObject.gameObject);
        nearestLightObject = null;
    }

    #region ScanOrbs

    void ScanWorldOrbs()
    {
        allLightSources = GameObject.FindGameObjectsWithTag("LightSource"); 
        
        for (int i = 0; i < allLightSources.Length; i++)
        {
            distance = Vector3.Distance(this.transform.position, allLightSources[i].transform.position);
            
            if (distance < nearestDistance)
            {
                   if (allLightSources[i].GetComponent<AnimateLightOrbFeedbackComponent>() == null)
                   {
                       Debug.Log("nearest object is an orb");
                       nearestLightObject = allLightSources[i];
                       nearestDistance = distance;
                   } 
                   
                   if (allLightSources[i].GetComponent<AnimateLightOrbFeedbackComponent>() != null 
                    && allLightSources[i].GetComponent<AnimateLightOrbFeedbackComponent>().isLightOn == true) 
                   {
                         Debug.Log("nearest object is a receptacle light");
                         nearestLightObject = allLightSources[i];
                         nearestDistance = distance;
                   }
                
                   if (allLightSources[i].GetComponent<AnimateLightOrbFeedbackComponent>() != null && 
                       (allLightSources[i].GetComponent<AnimateLightOrbFeedbackComponent>().isLightOn == false)) 
                    {
                        nearestLightObject = null;
                    }
                
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

    #region Pacify State Actions
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
        PetManager.Instance.AddCreature(this.gameObject);
    }

    #endregion

    #region Pet Action
    public IEnumerator OnPet()
    {
        if (canBePet)
        {
            canWander = false;
            canBePet = false;
            StartCoroutine(Scale(transform, canBePet));
            yield return new WaitForSeconds(3);
            canWander = true;
            Instantiate(newOrb,transform.position+Vector3.up*3, Quaternion.identity);
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
    #endregion

}
