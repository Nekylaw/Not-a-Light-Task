using UnityEngine;

public class MovementComponent : MonoBehaviour
{

    [SerializeField]
    private MovementSO _settings = null;

    private Rigidbody _rigidbody = null;

    private bool _isAiming = false;

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out _rigidbody))
            Debug.LogError($"{nameof(Rigidbody)} component not found.", this);
    }

    void Start()
    {
        if (_settings == null)
            Debug.LogError("Settings not found", this);
    }


    public bool Move(Vector3 direction, float delta, bool isAiming)
    {
        direction = _settings.UseProgressiveMove ? Vector3.ClampMagnitude(direction, 1) : direction.normalized;
  
        _isAiming = isAiming;

        float speed = isAiming ? _settings.Speed * _settings.SpeedRatioOnAim : _settings.Speed;

        _rigidbody.linearVelocity = direction * speed;

        return true;
    }
}
