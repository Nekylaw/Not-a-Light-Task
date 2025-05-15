using UnityEngine;

public class DetectionBehaviorComponent : MonoBehaviour
{

    #region Fields

    [SerializeField]
    private DetectionSettings _settings = null;

    private Rigidbody _rigidbody = null;

    private float _halfHeight = 0f;
    private bool _isGrounded = false;
    private bool _isTouchingWall = false;
    private Vector3 _groundNormal = Vector3.zero;

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (!TryGetComponent<Rigidbody>(out _rigidbody))
            Debug.LogError($"{nameof(Rigidbody)} component not found.", this);
    }

    void Start()
    {
        _halfHeight = GetComponent<Collider>().bounds.extents.y;
    }

    void FixedUpdate()
    {
        _isGrounded = CheckGround();
        _isTouchingWall = CheckWall();
    }

    #endregion

    #region Public API

    public bool IsGrounded => _isGrounded;
    public bool IsTouchingWall => _isTouchingWall;
    public Vector3 GroundNormal => _groundNormal;

    #endregion

    #region Private API

    private bool CheckGround()
    {
        Vector3 origin = _rigidbody.position;
        Vector3 direction = -transform.up;
        float distance = _halfHeight + _settings.DetectionRange;

        RaycastHit hit;
        if (Physics.SphereCast(origin, _settings.DetectionRange, direction, out hit, distance, _settings.JumpableLayers))
        {
            _groundNormal = hit.normal;
            return true;
        }

        _groundNormal = Vector3.up;
        return false;
    }


    private bool CheckWall()
    {
        Vector3 start = _rigidbody.position + Vector3.up * _halfHeight;
        Vector3 end = _rigidbody.position - Vector3.up * _halfHeight;
        return Physics.OverlapCapsule(start, end, _settings.WallCheckRadius, _settings.WallLayer).Length > 0;
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (_rigidbody == null || _settings == null)
            return;

        Vector3 feetPosition = _rigidbody.position - transform.up * _halfHeight;

        // Ground
        Gizmos.color = _settings.GroundCheckColor;
        Gizmos.DrawWireSphere(feetPosition, _settings.DetectionRange);

        //Wall
        Gizmos.color = _settings.WallCheckColor;
        Vector3 start = _rigidbody.position + Vector3.up * _halfHeight;
        Vector3 end = _rigidbody.position - Vector3.up * _halfHeight;
        Gizmos.DrawWireSphere(start, _settings.WallCheckRadius);
        Gizmos.DrawWireSphere(end, _settings.WallCheckRadius);
    }
}
