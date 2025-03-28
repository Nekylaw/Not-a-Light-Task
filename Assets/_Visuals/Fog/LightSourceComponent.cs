using UnityEngine;

public class LightSourceComponent : MonoBehaviour
{

    #region Fields

    private LightSourcesService _lightService = null;

    private bool _isLightOn = false;

    //@todo light settings asset

    #endregion


    #region Lifecycle

    private void Start()
    {
        _lightService = LightSourcesService.Instance;
        if (_lightService == null)
            Debug.LogWarning("Error, fogRenderer not found.", this);

        Debug.Log("Ligth Componrent ''-----------");

        Register();
    }

    private void OnEnable()
    {
        if (_lightService == null)
            return;

        Register();
    }

    private void OnDisable()
    {
        if (_lightService == null)
            return;

        Unregister();
    }

    #endregion


    #region Public API

    public bool IsLightOn => _isLightOn;

    public bool SwitchOn()
    {
        if (_isLightOn)
            return false;

        _isLightOn = CanLightOn();

        if (_isLightOn)
            Debug.Log("LIGHT ON");

        return _isLightOn;
    }

    public bool SwitchOff()
    {
        if (!_isLightOn)
            return false;

        _isLightOn = false;
        return true;
    }

    #endregion


    #region Private API

    private bool Register()
    {
        Debug.Log("Try register");

        if (_lightService == null)
        {
            Debug.Log("registering Flop");
            return false;
        }

        return _lightService.RegisterLightSource(this);
    }

    private bool Unregister()
    {
        if (_lightService == null)
            return false;

        return _lightService.UnregisterLightSource(this);
    }

    private bool CanLightOn()
    {
        //@todo check for light on
        return true;
    }

    #endregion

}
