using UnityEngine;

public class VortexEffectController : MonoBehaviour
{
    public Material vortexMaterial;
    public Transform centerWorldPosition;
    public Camera cam;

    void Update()
    {
        Vector3 screenPos = cam.WorldToViewportPoint(centerWorldPosition.position);
        vortexMaterial.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
    }
}
