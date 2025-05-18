using Services.Behaviors;
using Unity.VisualScripting;
using UnityEngine;

public class MovementBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private MovementSettings _settings = null;

    private Rigidbody _rigidbody = null;
    private DetectionBehaviorComponent _detector = null;
    private bool _isMoving = false;

    public bool IsMoving => _isMoving;

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
        float dot = Vector3.Dot(direction, Vector3.Cross(Vector3.down, _detector.GroundNormal));

        AdaptDirectionOnSlopes(ref direction);

        float speed = isAiming ? _settings.Speed * _settings.SpeedRatioOnAim : _settings.Speed;

        float slopeInfluence = Mathf.Lerp(1 - _settings.SlopeFactor, 1 + _settings.SlopeFactor, (dot + 1f) * 0.5f);

        Vector3 desiredVelocity = direction * speed * slopeInfluence;

        _isMoving = desiredVelocity.magnitude > 0.1f;
        if (direction.magnitude > 0.01f && GameManager.Instance.gameState == GameManager.GameState.Playing)
        {
            Vector3 accel = (desiredVelocity - _rigidbody.linearVelocity);
            _rigidbody.AddForce(accel * _settings.AccelerationFactor, ForceMode.Acceleration);
        }
        else
        {
            _rigidbody.linearVelocity = Vector3.zero;
            //Vector3 brakeForce = -_rigidbody.linearVelocity * _settings.DecelerationFactor;
            //_rigidbody.AddForce(brakeForce, ForceMode.Acceleration);
        }

        BehaviorsService.Move(direction, desiredVelocity.magnitude);

        return true;
    }


    private void AdaptDirectionOnSlopes(ref Vector3 direction)
    {
        if (!_detector.IsGrounded)
            return;

        direction = Vector3.ProjectOnPlane(direction, _detector.GroundNormal);
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
