using UnityEngine;

public class CameraBobbingComponent : MonoBehaviour
{
    [Header("Bobbing Settings")]
    public float Amplitude = 0.05f;
    public float Frequency = 10f;

    [Header("Axis")]
    public bool XAxis = false;
    [Range(0, 1)]
    public float XBobbingFactor = 1;

    public bool YAxis = true;
    [Range(0, 1)]
    public float YBobbingFactor = 1;

    [Header("Smoothing")]
    public float StopBobbingSpeed = 5f;

    private Vector3 _initialPosition;
    private float _bobbingTimer = 0f;

    private MovementBehaviorComponent _movementBehavior;
    private DetectionBehaviorComponent _detector;

    void Start()
    {
        _initialPosition = transform.localPosition;

        _detector = GetComponentInParent<DetectionBehaviorComponent>();
        _movementBehavior = GetComponentInParent<MovementBehaviorComponent>();

        if (!_movementBehavior || !_detector)
            Debug.LogWarning("Component missing", this);
    }

    void LateUpdate()
    {
        if (_movementBehavior == null || _detector == null)
            return;


        if (_movementBehavior.IsMoving && _detector.IsGrounded)
        {
            Debug.Log("Bobbing");
            Debug.Log("is moving" + _movementBehavior.IsMoving);

            _bobbingTimer += Time.deltaTime * Frequency;

            float verticalOffset = YAxis ? Mathf.Sin(_bobbingTimer) * Amplitude *  YBobbingFactor : 0f;
            float horizontalOffset = XAxis ? Mathf.Cos(_bobbingTimer * 0.5f) * Amplitude * XBobbingFactor : 0f;

            Vector3 offset = new Vector3(horizontalOffset, verticalOffset, 0f);
            transform.localPosition = _initialPosition + offset;
        }
        else
        {
            _bobbingTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _initialPosition, Time.deltaTime * StopBobbingSpeed);
        }
    }
}
