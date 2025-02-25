using System.Runtime.Serialization;
using UnityEngine;

public class JumpBehaviorComponent : MonoBehaviour
{
    [SerializeField]
    private JumpSettings _settings = null;

    private Rigidbody _rigidbody = null;

    private float _halfHeight = 0f;
    private bool _isJumping = false;

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out _rigidbody))
            Debug.LogError($"{nameof(Rigidbody)} component not found.", this);
    }

    void Start()
    {
        if (_settings == null)
            Debug.LogError($"{nameof(JumpSettings)} component not found", this);

        _halfHeight = GetComponent<Collider>().bounds.extents.y;
    }

    private void Update()
    {
        Debug.Log("Jumpping ???? " + _isJumping);
    }

    public bool Jump()
    {
        //if (!CanJump())
        //    return false;

        //_rigidbody.linearVelocity = new Vector3(_rigidbody.linearVelocity.x, 0, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(Vector3.up * _settings.JumpForce, ForceMode.Impulse);

        _isJumping = true;

        Debug.Log("Jump");

        return true;
    }

    private bool CanJump()
    {
        Vector3 feetPosition = _rigidbody.position - transform.up * _halfHeight;
        Collider[] colliders = Physics.OverlapSphere(feetPosition, _settings.DetectionRange, _settings.JumpableLayers);

        if (_rigidbody.linearVelocity.y <= 0)
            _isJumping = false;

        return colliders.Length > 0 && !_isJumping;
    }

    private void OnDrawGizmos()
    {
        if (_rigidbody == null)
            return;

        Vector3 feetPosition = _rigidbody.position - transform.up * _halfHeight;

        Gizmos.color = _settings.RadiusColor;
        Gizmos.DrawWireSphere(feetPosition, _settings.DetectionRange);
    }

}
