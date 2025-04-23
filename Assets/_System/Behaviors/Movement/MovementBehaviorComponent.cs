using Services.Behaviors;
using UnityEngine;
using UnityEngine.Android;

public class MovementBehaviorComponent : MonoBehaviour
{

    public float AccelerationFactor = 1;
    public float DecelerationFactor = 1;

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

        Vector3 desiredVelocity = direction * speed;

        if (direction.magnitude > 0.01f)
        {
            Vector3 accel = (desiredVelocity - _rigidbody.linearVelocity);
            _rigidbody.AddForce(accel * AccelerationFactor, ForceMode.Acceleration); //@todo cook ça 
        }
        else
        {
            Vector3 brakeForce = -_rigidbody.linearVelocity * DecelerationFactor;
            _rigidbody.AddForce(brakeForce, ForceMode.Acceleration);
        }


        BehaviorsService.Move(direction, speed);
        return true;
    }


    private Vector3 AdaptDirectionOnSlopes(Vector3 direction)
    {
        if (!_detector.IsGrounded)
            return direction;

        Vector3 slopeDir = Vector3.ProjectOnPlane(direction, _detector.GroundNormal);  //@todo change ground detection to get normal
        return slopeDir.normalized;
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
