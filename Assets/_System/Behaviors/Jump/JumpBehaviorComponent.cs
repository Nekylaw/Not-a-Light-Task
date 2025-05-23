using System.Runtime.Serialization;
using UnityEngine;

public class JumpBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private JumpSettings _settings = null;

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
            Debug.LogError($"{nameof(JumpSettings)} component not found", this);

    }

    public bool Jump()
    {
        if (!_detector.IsGrounded)
            return false;

        _rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(Vector3.up * _settings.JumpForce, ForceMode.Impulse);

        return true;
    }

}
