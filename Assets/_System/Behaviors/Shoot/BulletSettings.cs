using UnityEngine;

[CreateAssetMenu(fileName = "BulletSettings", menuName = "Game/Bullets/Light Bullet")]
public  class BulletSettings : ScriptableObject
{
    public GameObject Bullet = null;

    public float Lifetime = 0;

    public enum BulletType { Light, Pacify}
    public BulletType bulletType;

    public string bulletTag;
}
