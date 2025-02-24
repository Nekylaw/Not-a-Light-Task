using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettings", menuName = "Game/Behaviors/Movement Settings")]
public class MovementSO : ScriptableObject
{

    [Min(1)]
    public float Speed = 1f;

    [Range(0f, 1f)]
    public float SpeedRatioOnAim = 1f;

    public bool UseProgressiveMove = false;
}
