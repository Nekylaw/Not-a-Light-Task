using UnityEngine;

[CreateAssetMenu(fileName = "OrbContainerSettings", menuName = "Game/Orb Container")]
public class OrbContainerSettings : ScriptableObject
{
    public OrbComponent OrbPrefab;

    [Min(1)]
    public int MaxAmmo = 10;

    [Min(1)]
    public int BaseAmmo = 3;
}
