using System;
using System.Collections.Generic;
using UnityEngine;

public class LightSourcesService : MonoBehaviour, IDisposable
{

    #region Singleton

    public static LightSourcesService Instance { get; private set; }

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("Ligth Service ''-----------");
    }

    #endregion


    #region Delegates

    public delegate void SwitchOnLightDelegate(LightSourceComponent light);

    public delegate void SwitchOffLightDelegate(LightSourceComponent light);

    #endregion


    #region Fields

    public event SwitchOnLightDelegate OnSwitchOnLight = null;
    public event SwitchOffLightDelegate OnSwitchOffLight = null;

    private List<LightSourceComponent> _lightSourceList = new List<LightSourceComponent>();

    private bool _isDisposed = false;

    //@todo delete Test
    [SerializeField]
    private LightSourceComponent _lightTest = null;

    #endregion


    #region Lifecycle

    //@todo delete Test
    bool done = false;
    private void Update()
    {

        if (_lightTest != null && !done)
        {
            SwitchOn(_lightTest);
            done = true;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _lightSourceList.Clear();
        OnSwitchOnLight = null;
        OnSwitchOffLight = null;

        _isDisposed = true;
    }

    private void OnDestroy()
    {
        Dispose();
        if (Instance == this)
            Instance = null;
    }

    #endregion


    #region Public API

    public LightSourceComponent[] LightSourceList => _lightSourceList.ToArray(); //@todo IReadOnlyList<LightSourceComponent> ??
    public int LightSourceCount => _lightSourceList.Count;

    public bool SwitchOn(LightSourceComponent light)
    {
        if (!light.SwitchOn())
            return false;

        OnSwitchOnLight?.Invoke(light);
        return true;
    }

    public bool SwitchOff(LightSourceComponent light)
    {
        if (!light.SwitchOff())
            return false;

        OnSwitchOffLight?.Invoke(light);
        return true;
    }

    public bool RegisterLightSource(LightSourceComponent source)
    {
        if (_lightSourceList.Contains(source))
            return false;

        _lightSourceList.Add(source);

        Debug.Log("Ligth service light count: " + _lightSourceList.Count);
        return true;
    }

    public bool UnregisterLightSource(LightSourceComponent source)
    {
        if (!_lightSourceList.Contains(source))
            return false;

        _lightSourceList.Remove(source);
        return true;
    }

    #endregion


    #region Private API
    #endregion

}
