using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ChangeTexture : MonoBehaviour
{
    [SerializeField] private Texture2D newTexture;
    [SerializeField] private string texturePropertyName = "_TexFresque";

    private DecalProjector decalProjector;
    private Material decalMaterial;

    void Awake()
    {
        decalProjector = GetComponent<DecalProjector>();

        if (decalProjector.material == null)
        {
            Debug.LogWarning("Le DecalProjector n'a pas de material assigné.");
            return;
        }

        decalMaterial = new Material(decalProjector.material);
        decalMaterial.SetTexture(texturePropertyName, newTexture);
        decalProjector.material = decalMaterial;
    }
}
