using UnityEngine;

public class CameraBobbingComponent : MonoBehaviour, ICameraModifier
{
    [Header("Bobbing Settings")]
    public float Amplitude = 0.05f;
    public float Frequency = 10f;

    [Header("Axis")]
    public bool XAxis = false;
    [Range(0, 1)] public float XBobbingFactor = 1f;
    public bool YAxis = true;
    [Range(0, 1)] public float YBobbingFactor = 1f;

    [Header("Smoothing")]
    public float StartBobbingSpeed = 3f;
    public float StopBobbingSpeed = 5f;

    [Header("Behavior Options")]
    public bool DisableBobbingWhileAiming = false;

    private Vector3 _initialPosition;
    private float _bobbingTimer = 0f;
    private float _bobbingWeight = 0f;

    private MovementBehaviorComponent _movementBehavior;
    private DetectionBehaviorComponent _detector;
    private ShootBehaviorComponent _shootBehavior;

    /// <inheritdoc cref="ICameraModifier"/>
    public bool IsCameraLocked { get; set; }


    private void Start()
    {
        _initialPosition = transform.localPosition;

        _movementBehavior = GetComponentInParent<MovementBehaviorComponent>();
        _detector = GetComponentInParent<DetectionBehaviorComponent>();
        _shootBehavior = GetComponentInParent<ShootBehaviorComponent>();
    }

    private void LateUpdate()
    {
        if (IsCameraLocked)
            return;

        if (_movementBehavior == null || _detector == null || _shootBehavior == null)
            return;

        if (GameManager.Instance.gameState != GameManager.GameState.Playing)
            return;

        bool isMoving = _movementBehavior.IsMoving;
        bool isGrounded = _detector.IsGrounded;
        bool isAiming = _shootBehavior.IsAiming;

        bool shouldBob = isMoving && isGrounded && (!DisableBobbingWhileAiming || !isAiming);

        float targetWeight = shouldBob ? 1f : 0f;
        float lerpSpeed = shouldBob ? StartBobbingSpeed : StopBobbingSpeed;
        _bobbingWeight = Mathf.Lerp(_bobbingWeight, targetWeight, Time.deltaTime * lerpSpeed);

        float verticalOffset = YAxis ? Mathf.Sin(_bobbingTimer) * Amplitude * YBobbingFactor : 0f;
        float horizontalOffset = XAxis ? Mathf.Cos(_bobbingTimer * 0.5f) * Amplitude * XBobbingFactor : 0f;

        if (_bobbingWeight > 0.01f)
        {
            _bobbingTimer += Time.deltaTime * Frequency;

            Vector3 offset = new Vector3(horizontalOffset, verticalOffset, 0f) * _bobbingWeight;
            transform.localPosition = _initialPosition + offset;
        }
        else
        {
            _bobbingTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _initialPosition, Time.deltaTime * StopBobbingSpeed);
        }
    }
}
