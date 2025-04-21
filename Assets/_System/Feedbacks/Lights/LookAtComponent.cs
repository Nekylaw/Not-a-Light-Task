using UnityEngine;

public class LookAtComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _target = null;

    private Light _mainLight;  

    private void Start()
    {
        if (_target == null)
            Debug.LogWarning("Camera target not found.", this);

        _mainLight = GetComponent<Light>();

        if (_mainLight == null)
            Debug.LogWarning("No Light component found on this object.", this);
    }

    void LateUpdate()
    {
        if (_target == null || _mainLight == null)
            return;

        Vector3 directionToTarget = _target.position - _mainLight.transform.position;

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        _mainLight.transform.rotation = targetRotation; //@todo smooth settings
    }
}
