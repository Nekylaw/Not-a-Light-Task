using System;
using UnityEngine;

public class OrbContainerComponent : MonoBehaviour
{

    [SerializeField]
    private OrbContainerSettings _settings = null;

    private int _ammo = 0;

    void Start()
    {
        _ammo = Math.Clamp(_settings.BaseAmmo, 1, _settings.MaxAmmo); ; ;
    }

    public int Ammo => _ammo;
    public OrbComponent Orb => _settings.OrbPrefab;

    void Update() { }

    public bool CollectBullet(int amount)
    {
        if (amount <= 0)
            return false;

        _ammo += amount;
        _ammo = Mathf.Min(_settings.MaxAmmo, _ammo);

        return true;
    }

    public bool UseBullet(int amount = 1)
    {
        if (amount <= 0)
            return false;

        _ammo -= amount;
        _ammo = Mathf.Max(0, _ammo);

        return true;
    }
}
