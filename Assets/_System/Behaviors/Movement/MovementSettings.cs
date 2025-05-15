using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Game/Behaviors/Movement Settings")]
public class MovementSettings : ScriptableObject
{

    [Header("Speed")]

    [Min(1)]
    public float Speed = 1f;

    [Range(0f, 1f)]
    public float SpeedRatioOnAim = 1f;

    [Header("Fall")]

    [Min(0.1f)]
    public float FallingSpeed = 10f;

    [Min(0.1f)]
    public float GravityMultiplier = 1f;

    public bool UseProgressiveMove = false;

    [Header("Control")]

    [Min(0f)]
    public float AccelerationFactor = 1;

    [Min(0f)]
    public float DecelerationFactor = 1;

    [Range(0f, 1f)]
    public float SlopeFactor = 1;

    [Range(0, 1)]
    public float AirControl = 1f;

}
