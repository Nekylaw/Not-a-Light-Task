using UnityEngine;

[CreateAssetMenu(fileName = "JumpSettings", menuName = "Game/Behaviors/Jump Settings")]
public class JumpSettings : ScriptableObject
{

    [Min(1)]
    public float JumpForce = 5f;
    [Min(0)]
    public float DetectionRange = 5f;

    public LayerMask JumpableLayers = ~0;

    [Header("Debug")]
    public Color RadiusColor = Color.white;   
}
