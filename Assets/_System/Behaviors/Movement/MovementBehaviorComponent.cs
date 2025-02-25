using UnityEngine;

public class MovementBehaviorComponent : MonoBehaviour
{

    [SerializeField]
    private MovementSettings _settings = null;

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

        _rigidbody.linearVelocity = new Vector3(direction.x, _rigidbody.linearVelocity.y,direction.z) * speed;

        return true;
    }
}
