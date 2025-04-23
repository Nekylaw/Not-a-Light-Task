using UnityEngine;

[CreateAssetMenu(fileName ="PickupSettings", menuName = "Game/Behaviors/Pickup Settings")]
public class PickupSettings : ScriptableObject
{
    public LayerMask PickableLayer = ~0;

    public float PickupRange = 5;
}
