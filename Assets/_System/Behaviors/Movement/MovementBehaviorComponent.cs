using Services.Behaviors;
using Unity.VisualScripting;
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
    }

    public bool Move(Vector3 direction, float delta, bool isAiming)
    {
        direction = _settings.UseProgressiveMove ? Vector3.ClampMagnitude(direction, 1) : direction.normalized;
        float dot = Vector3.Dot(direction, Vector3.Cross(_detector.GroundNormal, Vector3.up));

        AdaptDirectionOnSlopes(ref direction);

        float speed = isAiming ? _settings.Speed * _settings.SpeedRatioOnAim : _settings.Speed;

        float slopeInfluence =  dot == 1f ? 1:  1 + dot * 2;

        Vector3 desiredVelocity = direction * speed * slopeInfluence;

        if (direction.magnitude > 0.01f)
        {
            Vector3 accel = (desiredVelocity - _rigidbody.linearVelocity);
            _rigidbody.AddForce(accel * _settings.AccelerationFactor, ForceMode.Acceleration);
        }
        else
        {
            Vector3 brakeForce = -_rigidbody.linearVelocity * _settings.DecelerationFactor;
            _rigidbody.AddForce(brakeForce, ForceMode.Acceleration);
        }

        BehaviorsService.Move(direction, speed);
        return true;
    }


    private void AdaptDirectionOnSlopes(ref Vector3 direction)
    {
        if (!_detector.IsGrounded)
            return;

        direction = Vector3.ProjectOnPlane(direction, _detector.GroundNormal);
        return;
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
