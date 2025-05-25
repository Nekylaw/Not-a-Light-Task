using TMPro;
using UnityEngine;

public class PacifyTrail : MonoBehaviour
{
    [SerializeField] private Transform trail;
    [SerializeField] private Transform trailPrefab;
    
    
    
    void Update()
    {
        trail.transform.RotateAround(transform.position, Vector3.up, 5f);
        trail.position = Vector3.Slerp(trail.transform.position, transform.position, 0.1f * Time.deltaTime);
        if (Vector3.Distance(trail.transform.position, transform.position) < 0.9f)
        {
            Destroy(trail.gameObject);
            trail = Instantiate(trailPrefab, transform);
            transform.position = new Vector3(0, 0, 0);
        }
        transform.Translate(0, 0.5f * Time.deltaTime, 0);
    }
}
