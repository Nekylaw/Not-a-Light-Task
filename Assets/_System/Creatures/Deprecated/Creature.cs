using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Creature : MonoBehaviour
{
    private bool _isInRange, _isVisible, _isInAngle;
    [SerializeField] Collider receptacle;
    public float rangeReceptacleDetection = 10;
    private void Update()
    {
        CreatureVision();
    }
    

    private void CreatureVision()
    {
        if (Vector3.Distance(transform.position, receptacle.transform.position) < rangeReceptacleDetection)
        {
            _isInRange = true;
            Debug.Log("In Range" + Vector3.Distance(transform.position, receptacle.transform.position));
        }
    }
    
    private void MoveToLight()
    {
                
    }
    
    
}
