using UnityEngine;

[CreateAssetMenu(fileName = "LightSurceSettings", menuName = "Game/Light Sources")]
public class LightSourceSettings : ScriptableObject
{
    public float AttractRange = 0f;

    public float BrightnessRange = 0f;

    public LayerMask OrbLayer = ~0;

    public int RequiredOrbs = 0;

}
