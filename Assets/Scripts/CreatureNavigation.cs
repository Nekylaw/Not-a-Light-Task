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

    private bool canEatLight = false;

    // Use this for initialization
    void OnEnable () {
        creature = GetComponent<NavMeshAgent> ();
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
        if (objs.Count >= 1)
        {
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

            if (canEatLight == true)
            {
                obj.SetActive(false);
                Debug.Log("Light touched: creature eat it");
            }
        }
    }

    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Orb"))
        {
            canEatLight = true;
            Debug.Log("collision light");
        }
    }


    void FaceTarget(Vector3 targetPosition )
    {
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(targetPosition.x, 0, targetPosition.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5);
    }
    
}
