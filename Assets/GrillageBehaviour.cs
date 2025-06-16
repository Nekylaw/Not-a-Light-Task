using UnityEngine;

public class GrillageBehaviour : MonoBehaviour
{
    [SerializeField] private Transform pivot;

    
    public void OpenGate()
    {
        transform.RotateAround(pivot.position, Vector3.up, 150f);
    }
}
