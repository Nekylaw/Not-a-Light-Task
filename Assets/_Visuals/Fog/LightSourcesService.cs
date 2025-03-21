using System.Collections.Generic;
using UnityEngine;

public class LightSourcesService : MonoBehaviour
{

    public delegate void SwitchOnLightDelegate(LightSourceComponent light);
    public delegate void SwitchOffLightDelegate(LightSourceComponent light);

    public event SwitchOffLightDelegate OnSwitchOnLight = null;
    public event SwitchOffLightDelegate OnSwitchOffLight = null;

    private FogRenderer _fogRenderer = null;
    private List<LightSourceComponent> _lightSourceOnList = new List<LightSourceComponent>();


    [SerializeField]
    private LightSourceComponent _lightTest = null;

    private void Start()
    {
        _fogRenderer = FindFirstObjectByType<FogRenderer>();

        if (_fogRenderer == null)
            Debug.LogWarning("Error, fogRenderer not found.", this);


        SwitchOn(_lightTest);
    }

    public List<LightSourceComponent> LightSourceList => _lightSourceOnList;

    public int LightOnCount => _lightSourceOnList.Count;

    public bool SwitchOn(LightSourceComponent light)
    {
        if (!light.SwitchOn())
            return false;

        RegisterLightSource(light);

        OnSwitchOnLight?.Invoke(light);
        return true;
    }

    public bool SwitchOff(LightSourceComponent light)
    {
        if (!light.SwitchOff())
            return false;

        UnregisterLightSource(light);

        OnSwitchOffLight?.Invoke(light);
        return true;
    }

    private bool RegisterLightSource(LightSourceComponent source)
    {
        if (_lightSourceOnList.Contains(source))
            return true;

        _lightSourceOnList.Add(source);
        return true;
    }

    private bool UnregisterLightSource(LightSourceComponent source)
    {
        if (!_lightSourceOnList.Contains(source))
            return false;

        _lightSourceOnList.Remove(source);
        return true;
    }

}
