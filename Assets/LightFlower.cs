using System;
using System.Collections.Generic;
using UnityEngine;

public class LightFlower : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Material glowMat;
    private List<int> _indexes = new();

    [SerializeField] private float activationDistance;

    private void Update()
    {
        if (player == null || glowMat == null)
            return;
        
        CheckProximity();
    }

    private void CheckProximity()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= activationDistance)
        {
            EnlightFlower();
        }
    }

    private void EnlightFlower()
    {
        if (gameObject.GetComponent<MeshRenderer>() != null)
        {
            var _materials = gameObject.GetComponent<MeshRenderer>().materials;

            if (_materials.Length != 0)
            {
                for (var i = 0; i < _materials.Length; i++)
                {
                    if (_materials[i].name.ToLower().Contains("flower") ||
                        _materials[i].name.ToLower().Contains("center"))
                    {
                        Debug.Log("added" + i);
                        _indexes.Add(i);
                    }
                }

                foreach (var index in _indexes)
                {
                    _materials[index] = glowMat;
                }

                gameObject.GetComponent<MeshRenderer>().materials = _materials;
            }
        }

    }
}
