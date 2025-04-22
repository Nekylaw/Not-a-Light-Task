using UnityEngine;

public class ShootBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _firePoint = null;

    [SerializeField]
    private ShootSettings _settings = null;

    private OrbContainerComponent _container = null;

    private float _timer = 0;

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
    }

    private void Update()
    {
        _timer -= Time.deltaTime;

        if (_timer <= 0)
            _timer = 0;
    }

    public bool Shoot(Ray aimRay)
    {
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
}
