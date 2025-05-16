using System;
using Game.Services.LightSources;
using UnityEngine;

public class CreatureSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _creaturePrefab;
    [SerializeField] private int maxCreatures;
    [SerializeField] private int creaturesAmount;
    private LightSourceComponent _lightSource;

    private void Start()
    {
        _lightSource = GetComponent<LightSourceComponent>();
    }

    public void CheckIfSpawn()
    {
        if (_lightSource.IsLightOn)
            return;
        if (creaturesAmount >= maxCreatures)
            return;
        
        SpawnCreature();
    }

    public void SpawnCreature()
    {
        Instantiate(_creaturePrefab, transform.position, Quaternion.identity);
        creaturesAmount++;
    }
}
