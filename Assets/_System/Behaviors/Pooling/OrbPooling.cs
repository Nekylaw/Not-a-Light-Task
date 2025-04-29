using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class OrbPooling : MonoBehaviour
{
   public static OrbPooling Instance;
   public List<GameObject> poolOrbs;
   public GameObject orbPrefab;
   public OrbComponent activeOrb;
   public int maxOrbs;

   void Awake()
   {
      Instance = this;
   }

   public void InitializeOrbs()
   {
      poolOrbs = new List<GameObject>();
      GameObject orb;

      for (int i = 0; i < maxOrbs; i++)
      {
         orb = Instantiate(orbPrefab);
         orb.SetActive(false);
         poolOrbs.Add(orb);
      }
   }

   public OrbComponent Get()
   {
      for (int i = 0; i < poolOrbs.Count; i++)
      {
         if (!poolOrbs[i].activeInHierarchy)
         {
            activeOrb = poolOrbs[i].GetComponent<OrbComponent>();
            return activeOrb;
         }
      }
      return null;
   }
   
}
