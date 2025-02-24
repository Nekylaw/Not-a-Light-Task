using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "Game/Camera/Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Sensitivity")]
    public float YawSensitivity = 200;

    public float PitchSensitivity = 200;

    [Header("Limits")]
    public Vector2 RotationLimits = new Vector2(-30, 45);

    [Header("Axes")]
    public bool InverseYaxe = false;
}
