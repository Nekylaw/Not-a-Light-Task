using UnityEngine;

[CreateAssetMenu(fileName = "DetectionSettings", menuName = "Game/Behaviors/Detector")]
public class DetectionSettings : ScriptableObject
{
    [Header("Detection - Ground")]
    [Min(0)]
    public float DetectionRange = 0.2f;
    public LayerMask JumpableLayers = ~0;

    [Header("Detection - Wall")]
    public float WallCheckRadius = 0f;
    public LayerMask WallLayer = ~0;

    [Header("Debug")]
    public Color GroundCheckColor = Color.green;
    public Color WallCheckColor = Color.red;
}
