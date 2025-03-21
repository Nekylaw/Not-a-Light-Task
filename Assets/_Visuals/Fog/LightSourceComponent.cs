using UnityEngine;

public class LightSourceComponent : MonoBehaviour
{
    private bool _isLightOn = false;

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

    public bool CanLightOn()
    {
        //@todo check for light on
        return true;
    }

}
