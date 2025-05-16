using UnityEngine;

[CreateAssetMenu(fileName = "LightSurceSettings", menuName = "Game/Light Sources")]
public class LightSourceSettings : ScriptableObject
{
    [Header("General")]

    public float BrightnessRange = 0f;

    public int RequiredOrbs = 0;

    public LayerMask OrbLayer = ~0;

    public float DissipationSpeed = 0f; 

    [Header("Orb Attraction")]
    
    public float AttractRange = 0f;

    public float Duration = 0;

    public float SpiralSpeed = 0;
}
