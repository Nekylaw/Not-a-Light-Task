using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class ShootBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _firePoint = null;

    [SerializeField]
    private Transform _gun = null;

    [SerializeField]
    private Vector3 _aimGunRotation = Vector3.zero;

    [SerializeField]
    private Vector3 _aimGunPosition = Vector3.zero;

    [SerializeField]
    private ShootSettings _settings = null;

    private OrbContainerComponent _container = null;

    private float _timer = 0;

    private Quaternion _aimGunRotationQuaternion = Quaternion.identity;
    private Quaternion _gunStartRotation = Quaternion.identity;
    private Vector3 _gunStartPosition = Vector3.zero;
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
        _aimGunRotationQuaternion = Quaternion.Euler(_aimGunRotation);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0)
            _timer = 0;

        if (_isAiming)
            HandleAim();
        else
            ReleaseAim();
    }

    private void ReleaseAim()
    {
        _gun.localRotation = Quaternion.Slerp(_gun.localRotation, _gunStartRotation, Time.deltaTime * _settings.AimSpeed);
        _gun.localPosition = Vector3.Lerp(_gun.localPosition, _gunStartPosition, Time.deltaTime * _settings.AimSpeed);
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

        Debug.DrawRay(_firePoint.position, aimRay.direction * 50, Color.yellow, 1f);
        Debug.Log("SHOOT");

        return true;
    }

    private void HandleAim()
    {
        _gun.localRotation = Quaternion.Slerp(_gun.localRotation, _aimGunRotationQuaternion, Time.deltaTime * _settings.AimSpeed);
        _gun.localPosition = Vector3.Lerp(_gun.localPosition, _aimGunPosition, Time.deltaTime * _settings.AimSpeed);
    }

}
