using UnityEngine;

[CreateAssetMenu(fileName = "OrbSettings", menuName = "Game/Orb/Light Orb")]
public class OrbSettings : ScriptableObject
{
    public bool HasLifetime = false;

    [Min(0)]
    public float Lifetime = 0;
}
