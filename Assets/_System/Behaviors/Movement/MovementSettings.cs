using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Game/Behaviors/Movement Settings")]
public class MovementSettings : ScriptableObject
{

    [Min(1)]
    public float Speed = 1f;

    [Min(0.1f)]
    public float FallingSpeed = 10f;

    [Min(0.1f)]
    public float GravityMultiplier = 1f;

    [Range(0f, 1f)]
    public float SpeedRatioOnAim = 1f;

    [Range(0, 1)]
    public float AirControl = 1f;

    public bool UseProgressiveMove = false;
}
