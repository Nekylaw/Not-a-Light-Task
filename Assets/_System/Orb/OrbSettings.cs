using UnityEngine;

[CreateAssetMenu(fileName = "OrbSettings", menuName = "Game/Orb/Light Orb")]
public class OrbSettings : ScriptableObject
{
    [Header("Eating Settings")]
    public float EatingEffectDuration = 0.5f;
    public AnimationCurve ScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public  AnimationCurve AlphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
}
