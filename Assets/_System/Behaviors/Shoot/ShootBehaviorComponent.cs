using UnityEngine;

public class ShootBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _firePoint = null;

    [SerializeField]
    private Transform _gun = null;

    [SerializeField]
    private ShootSettings _settings = null;

    [SerializeField]
    private RectTransform _crossHairRect = null;

    private OrbContainerComponent _container = null;

    private Quaternion _aimGunRotationQuaternion = Quaternion.identity;
    private Quaternion _gunStartRotation = Quaternion.identity;
    private Vector3 _gunStartPosition = Vector3.zero;

    private float _timer = 0;
    private bool _isAiming;

    public bool IsAiming
    {
        get => _isAiming;
        set { _isAiming = value; }
    }

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError($"{nameof(ShootSettings)} component not found", this);

        if (!TryGetComponent<OrbContainerComponent>(out _container))
            Debug.LogError($"{nameof(OrbContainerComponent)} component not found", this);
    }

    private void Start()
    {
        _timer = _settings.Rate;

        _gunStartRotation = _gun.localRotation;
        _gunStartPosition = _gun.localPosition;

        //Cast to Quaternion
        _aimGunRotationQuaternion = Quaternion.Euler(_settings.AimGunRotation);
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        _timer -= delta;

        if (_timer <= 0)
            _timer = 0;

        if (_isAiming)
            HandleAim(delta);
        else
            ReleaseAim(delta);
    }

    public bool Shoot(Ray aimRay, bool isAiming)
    {
        _isAiming = isAiming;

        if (!isAiming)
            return false;

        if (_container == null)
            return false;

        if (_timer > 0)
            return false;

        if (_container.Ammo <= 0)
            return false;

        _timer = _settings.Rate;
        _container.UseBullet();

        OrbComponent bullet = Instantiate(_container.Orb, _firePoint.position + aimRay.direction * 0.2f, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().AddForce(aimRay.direction * _settings.FireForce, ForceMode.Impulse);

        return true;
    }

    private void HandleAim(float delta)
    {
        _gun.localRotation = Quaternion.Slerp(_gun.localRotation, _aimGunRotationQuaternion, delta * _settings.AimSpeed);
        _gun.localPosition = Vector3.Lerp(_gun.localPosition, _settings.AimGunPosition, delta * _settings.AimSpeed);

        if (_crossHairRect != null)
        {
            _crossHairRect.rotation = Quaternion.Slerp(_crossHairRect.rotation, Quaternion.Euler(0, 0, 90), delta * _settings.AimSpeed);
            _crossHairRect.localScale = Vector3.Lerp(_crossHairRect.localScale, new Vector3(0.6f, 0.6f, 0.6f), delta * _settings.AimSpeed);
        }
    }

    private void ReleaseAim(float delta)
    {
        _gun.localRotation = Quaternion.Slerp(_gun.localRotation, _gunStartRotation, delta * _settings.AimSpeed);
        _gun.localPosition = Vector3.Lerp(_gun.localPosition, _gunStartPosition, delta * _settings.AimSpeed);

        if (_crossHairRect != null)
        {
            _crossHairRect.rotation = Quaternion.Slerp(_crossHairRect.rotation, Quaternion.Euler(0, 0, 0), delta * _settings.AimSpeed);
            _crossHairRect.localScale = Vector3.Lerp(_crossHairRect.localScale, new Vector3(0.8f, 0.8f, 0.8f), delta * _settings.AimSpeed);
        }
    }

}
