using System.Collections.Generic;
using UnityEngine;

public class EnlightTower : MonoBehaviour
{
    [SerializeField] private Material _materialLight;
    private List<int> _indexes = new();

    public void LightTower()
    {
        var _materials = gameObject.GetComponent<MeshRenderer>().materials;

        if (_materials.Length != 0)
        {
            for (var i = 0; i < _materials.Length; i++)
            {
                if (_materials[i].name.ToLower().Contains("lamp") || _materials[i].name.ToLower().Contains("window"))
                {
                    Debug.Log("added" + i);
                    _indexes.Add(i);
                }
            }

            foreach (var index in _indexes)
            {
                _materials[index] = _materialLight;
            }

            gameObject.GetComponent<MeshRenderer>().materials = _materials;
        }

        var enfants = GetComponentsInChildren<EnlightTower>();

        foreach (var VARIABLE in enfants)
        {
            if (VARIABLE != this)
                VARIABLE.LightTower();
        }
    }
    
}
