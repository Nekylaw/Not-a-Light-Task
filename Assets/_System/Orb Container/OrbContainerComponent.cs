using System;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class OrbContainerComponent : MonoBehaviour
{

    [SerializeField] private OrbContainerSettings _settings = null;
    [SerializeField] private GameObject orbSunUI;
    private int orbIndexUI = 0;
    [SerializeField] private Sprite orbSprite;
    
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
        
        orbSunUI.transform.GetChild(orbIndexUI).gameObject.SetActive(true);
        orbIndexUI++;
 
        return true;
    }

    public bool UseBullet(int amount = 1)
    {
        if (amount <= 0)
            return false;

        _ammo -= amount;
        _ammo = Mathf.Max(0, _ammo);
        Debug.Log("ammo left: " + _ammo);

        orbIndexUI = _ammo;
        orbSunUI.transform.GetChild(orbIndexUI).gameObject.SetActive(false);

        return true;
    }
}
