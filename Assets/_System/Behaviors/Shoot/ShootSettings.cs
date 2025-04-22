using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ShootSettings", menuName = "Game/Behaviors/Shoot Settings")]
public class ShootSettings : ScriptableObject
{
    public float FireForce = 0;

    public float Rate = 0f;

    public float ReloadDuration = 0f;
}
