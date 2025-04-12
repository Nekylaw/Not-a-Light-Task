using UnityEngine;

public class ShootBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _firePoint = null;

    [SerializeField]
    private ShootSettings _settings = null;

    private float _timer = 0;

    private void Awake()
    {
        if (_settings == null)
            Debug.LogError($"{nameof(ShootSettings)} component not found", this);
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
        if (_timer > 0)
            return false;

        _timer = _settings.Rate;

        Debug.DrawRay(_firePoint.position, aimRay.direction * 50, Color.yellow, 1f);

        //if (Physics.Raycast(aimRay, out RaycastHit hit))
        //    Debug.Log("Shoot on " + hit.collider.name);

        GameObject bullet = Instantiate(_settings.Bullets[0].Bullet, _firePoint.position + aimRay.direction * 0.2f, Quaternion.identity);

        //@todo make the bullet shoot itself
        bullet.GetComponent<Rigidbody>().AddForce(aimRay.direction * _settings.FireForce, ForceMode.Impulse);

        return true;
    }
}
