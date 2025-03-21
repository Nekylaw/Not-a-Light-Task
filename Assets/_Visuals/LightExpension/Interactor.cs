using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField]
   private float radius;

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalVector("_Position", transform.position);
        Shader.SetGlobalFloat("_Radius", radius);
    }
}