using UnityEngine;

[CreateAssetMenu(fileName = "JumpSettings", menuName = "Game/Behaviors/Jump Settings")]
public class JumpSettings : ScriptableObject
{

    [Min(1)]
    public float JumpForce = 5f;

}
