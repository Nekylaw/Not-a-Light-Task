using Services.Behaviors;
using UnityEngine;

public class MovementBehaviorComponent : MonoBehaviour
{

    [SerializeField]
    private MovementSettings _settings = null;

    private Rigidbody _rigidbody = null;
    private DetectionBehaviorComponent _detector = null;

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out _rigidbody))
            Debug.LogError($"{nameof(Rigidbody)} component not found.", this);

        if (!TryGetComponent<DetectionBehaviorComponent>(out _detector))
            Debug.LogError($"{nameof(DetectionBehaviorComponent)} component not found.", this);
    }

    void Start()
    {
        if (_settings == null)
            Debug.LogError("Settings not found", this);
    }

    private void FixedUpdate()
    {
        HandleFalling();
        AdjustPosition();
    }

    public bool Move(Vector3 direction, float delta, bool isAiming)
    {
        direction = _settings.UseProgressiveMove ? Vector3.ClampMagnitude(direction, 1) : direction.normalized;
        float speed = isAiming ? _settings.Speed * _settings.SpeedRatioOnAim : _settings.Speed;

        _rigidbody.linearVelocity = new Vector3(direction.x * speed, _rigidbody.linearVelocity.y, direction.z * speed);

        BehaviorsService.Move(direction, speed);
        return true;
    }

    private void AdjustPosition()
    {
    }

    private void HandleFalling()
    {
        if (_detector.IsGrounded)
            return;

        _rigidbody.AddForce(_settings.GravityMultiplier * Physics.gravity, ForceMode.Acceleration);

        _rigidbody.linearVelocity = new Vector3
            (
            _rigidbody.linearVelocity.x * _settings.AirControl,
            Mathf.Max(_rigidbody.linearVelocity.y, -_settings.FallingSpeed),
            _rigidbody.linearVelocity.z * _settings.AirControl
            );
    }

}
