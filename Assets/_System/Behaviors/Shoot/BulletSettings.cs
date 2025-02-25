using UnityEngine;

[CreateAssetMenu(fileName = "BulletSettings", menuName = "Game/Bullets/Light Bullet")]
public  class BulletSettings : ScriptableObject
{
    public GameObject Bullet = null;

    public float Lifetime = 0;
}
