using System;
using System.Collections.Generic;
using UnityEditor.XR;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public class CreatureNavigation : MonoBehaviour
{
    public float wanderRadius;
    public float wanderTimer;

    private Transform target;
    private NavMeshAgent creature;
    private float timer;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask lightLayer;

    private List<GameObject> objs = CreatureFOV.objectsInSight;
    private List<GameObject> orbsEaten = new List<GameObject>();

    private CreatureState _state ;
    

    // Use this for initialization
    void OnEnable () 
    {
        creature = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
    }
  
    // Update is called once per frame
    void Update () {
        timer += Time.deltaTime;
  
        if (timer >= wanderTimer && objs.Count == 0) 
        {
            Debug.Log("No light source detected : wander behaviour");
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            creature.SetDestination(newPos);
            timer = 0;  
            transform.LookAt(newPos);
        }
        if (objs.Count >= 1 && GetComponent<CreatureState>().isEvil)
        {
            Debug.Log("Light detected : creature walks toward it");
            GoToLight();
        }
       
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
        Vector3 randDirection = Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition (randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
    
    
    public void GoToLight()
    { 
        var step = 3 * Time.deltaTime;
     
        foreach (var obj in objs)
        {  
            if (obj.layer == 6)
            { 
                transform.position = Vector3.MoveTowards(transform.position, obj.transform.position, step);
                Debug.Log("Light detected : creature walks toward it");
            }
            
        }
    }

    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Orb") && objs.Contains(other.gameObject))
        {
            other.gameObject.SetActive(false);
            orbsEaten.Add(other.gameObject);
            Debug.Log("Creature eat light");
        }
    }


    void FaceTarget(Vector3 targetPosition )
    {
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(targetPosition.x, 0, targetPosition.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);
    }
    
}
