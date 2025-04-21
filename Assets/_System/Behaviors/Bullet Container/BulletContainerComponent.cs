using UnityEngine;

public class BulletContainerComponent : MonoBehaviour
{

    private int _ammo = 0;

    public int MaxAmmo = 10;

    void Start()
    {

    }

    void Update()
    {

    }

    public bool CollectBullet(int amount)
    {
        if (amount <= 0)
            return false;

        _ammo += amount;
        _ammo = Mathf.Min(MaxAmmo, _ammo);

        return true;
    }

    public bool ReleaseBullet(int amount)
    {
        if (amount <= 0)
            return false;

        _ammo -= amount;
        _ammo = Mathf.Max(0, _ammo);

        return true;
    }
}
