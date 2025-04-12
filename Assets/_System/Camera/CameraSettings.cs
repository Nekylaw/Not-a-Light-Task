using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "Game/Camera/Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Sensitivity - Gamepad ")]
    public float ControllerYawSensitivity = 200f;
    public float ControllerPitchSensitivity = 200f;

    [Header("Sensitivity - Mouse")]
    public float MouseYawSensitivity = 200f;
    public float MousePitchSensitivity = 200f;

    [Header("Limits")]
    public Vector2 RotationLimits = new Vector2(-30, 45);

    [Header("Axes")]
    public bool InverseYaxe = false;
}
