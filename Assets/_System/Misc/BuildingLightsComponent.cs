using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingLightsComponent : MonoBehaviour
{
    [SerializeField] private Material _materialLight;
    private List<int> _indexes = new();

    public void LightBuilding()
    {
        var materials = gameObject.GetComponent<MeshRenderer>().materials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].name.ToLower().Contains("lamp") || materials[i].name.ToLower().Contains("window"))
            {
                Debug.Log("added" + i);
                _indexes.Add(i);
            }
        }

        foreach (var index in _indexes)
        {
            materials[index] = _materialLight;
        }
        
        gameObject.GetComponent<MeshRenderer>().materials = materials;
    }

}
