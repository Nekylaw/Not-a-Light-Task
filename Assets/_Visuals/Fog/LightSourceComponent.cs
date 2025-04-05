using UnityEngine;

public class LightSourceComponent : MonoBehaviour
{

    #region Fields

    private LightSourcesService _lightService = null;

    [SerializeField]
    private Transform _lightPoint = null;

    private bool _isLightOn = false;

    //@todo light settings asset

    // public orb required to ligth on setting
    public float AttractRange = 0f;

    #endregion


    #region Lifecycle

    private void Awake()
    {
        //_lightService = LightSourcesService.Instance;
        //if (_lightService == null)
        //    return;

        //Register();
    }

    private void Start()
    {
        //if (_lightService == null)
        //    return;

        //@todo remove
        AttractRange = 2;
    }

    private void Update()
    {
        DetectBullet();
    }

    private void OnEnable()
    {
        Debug.Log("Enable light Compoennt ");

        Register();
    }

    private void OnDisable()
    {
        Unregister();
    }

    private bool Register()
    {
        if (_lightService == null)
        {
            Debug.Log("Init light Compoennt before the service ");
            //return false;
        }
        else
            Debug.Log("Init light Compoennt SUCCESS");


        _lightService = LightSourcesService.Instance;

        return _lightService.RegisterLightSource(this);
    }

    private bool Unregister()
    {
        if (_lightService == null)
            return false;

        return _lightService.UnregisterLightSource(this);
    }

    #endregion


    #region Public API

    public bool IsLightOn => _isLightOn;

    public Vector3 LightPoint => _lightPoint.position;

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

    private bool DetectBullet()
    {
        if (_isLightOn)
            return false;

        Collider[] colliders = Physics.OverlapSphere(_lightPoint.position, AttractRange); //@todo bullet layer

        if (colliders.Length <= 0)
            return false;

        foreach (Collider collider in colliders)
        {
            if (!collider.TryGetComponent<BulletComponent>(out BulletComponent bullet))
                continue;

            Debug.Log("Hittttttt");
            bullet.AttractTo(_lightPoint.position, this);
        }
        return true;
    }

    private bool CanLightOn()
    {
        //@todo check for light on
        return true;
    }

    #endregion


    #region Debug

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_lightPoint.position, AttractRange);
    }

    #endregion

}
