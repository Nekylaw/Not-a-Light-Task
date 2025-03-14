using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogRenderer : MonoBehaviour
{
    private Material fogMaterial;

    // Liste des points de dissipation
    public Vector4[] dissipationPoints;

    private void Start()
    {
        // Charger le shader de brouillard volumétrique
        Shader fogShader = Shader.Find("Effect/Fog");

        if (fogShader == null)
            Debug.Log("Error");
        else
            Debug.Log("Found");


        fogMaterial = new Material(fogShader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Mettre à jour les points de dissipation dans le shader
        fogMaterial.SetVectorArray("_FogDissipationPoints", dissipationPoints);
        // Appliquer le shader de brouillard volumétrique
        Graphics.Blit(source, destination, fogMaterial);
    }
}
