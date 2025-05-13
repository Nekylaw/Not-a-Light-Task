using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ShootSettings", menuName = "Game/Behaviors/Shoot Settings")]
public class ShootSettings : ScriptableObject
{
    [Header("Shoot")]
    public float FireForce = 0;

    public float Rate = 0f;

    public float ReloadDuration = 0f;

    public float AimSpeed = 1f;

    [Header("Aim")]
    public Vector3 AimGunRotation = Vector3.zero;

    public Vector3 AimGunPosition = Vector3.zero;
}
