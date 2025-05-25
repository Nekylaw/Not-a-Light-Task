using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
public class NEW_IAController : MonoBehaviour


{  [SerializeField] public GameObject pacifyEffects;
   
   public GameObject[] allOrbs;
   public GameObject nearestObject; 
   public List<GameObject> orbsEaten;
   
   float distance;
   float nearestDistance = 100;
   
   public bool canWander = false;
   public bool isBeingPacified = false;
   public bool isPacified = false;
   
   private CreatureState creatureState = new CreatureState();
   
   
   void Update()
   {
      // if (nearestObject != null && creatureState.isEvil == true && canWander)
      // {
      //    MoveTo(nearestObject.transform.position);
      // }
      // else
      // {
      //    if (creatureState.isEvil == true && canWander)
      //    {
      //       ScanWorldOrbs();
      //    }
      //    
      //    if (canWander)
      //    {
      //       WanderBehaviour();
      //    }
      //    
      // }
   }

   private void OnCollisionEnter(Collision other)
   {
      if (other.gameObject.CompareTag("Orb") && creatureState.isEvil)
      {
         other.gameObject.SetActive(false);
         nearestObject = null;
         Debug.Log("orb eaten by: " + this.name);
         orbsEaten.Add(other.gameObject);
      }

      // if (other.gameObject.CompareTag("Pacify"))
      // {
      //    creatureState.isEvil = false;
      //    foreach (var orb in orbsEaten)
      //    {
      //       orb.SetActive(true);
      //       orb.transform.position = this.transform.position;
      //       Debug.Log("creature pacified : orb given back !"); 
      //    }
      //    orbsEaten.Clear();
      // }
   }
   
   

   #region ScanOrbs

   void ScanWorldOrbs()
   {
      allOrbs = GameObject.FindGameObjectsWithTag("Orb");
       for (int i = 0; i < allOrbs.Length; i++)
       {
          distance = Vector3.Distance(this.transform.position , allOrbs[i].transform.position);
          if (distance < nearestDistance)
          {
             nearestObject = allOrbs[i];
             nearestDistance = distance;
             Debug.Log("new nearest orb: " + nearestObject.name);
          }
       }
   }
   
   

   #endregion
   
   #region Wander
   
   [SerializeField] float circRadius = 10f;
   [SerializeField] float circDistance = 10f;	
   [SerializeField] float wanderRandomizer = 1f;

   Vector3 aiWanderGoal = Vector3.zero;

   public void WanderBehaviour() {
      if (canWander)
      {
         aiWanderGoal += new Vector3( Random.Range(-1.0f, 1.0f) * wanderRandomizer, 0f, Random.Range(-1.0f, 1.0f) * wanderRandomizer );
         aiWanderGoal.Normalize(); 
         aiWanderGoal *= circRadius;
         var locTarget = aiWanderGoal + new Vector3(0, 0, circDistance);
         var worldCoord = gameObject.transform.InverseTransformVector(locTarget);
         
         MoveTo(worldCoord);
      }
      else
      {
         
      }
      
      
   }
   
   
   
   void MoveTo( Vector3 location ) 
   {
      NavMeshAgent agent = GetComponent<NavMeshAgent>();
      agent.SetDestination( location );
   }
   #endregion


   public void StartPacifyEffects()
   {
      if (isBeingPacified)
      {
         pacifyEffects.SetActive(true);
      }
   }
   
   public IEnumerator OnEndPacify(GameObject target)
   {
      yield return new WaitForSeconds(5);  
      pacifyEffects.SetActive(false);
      target.GetComponent<NEW_IAController>().isPacified = true;
      target.GetComponent<NEW_IAController>().isBeingPacified = false;
   }
   
}
