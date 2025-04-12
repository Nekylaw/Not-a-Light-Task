using UnityEngine;

public class OrbitalEffectComponent : MonoBehaviour
{

    [SerializeField]
    private Transform _center = null;

    [SerializeField]
    private float radius = 0f;

    [SerializeField]
    private float _speed = 0f;

    [SerializeField]
    [Range(1, 1.5f)]
    private float _axisOffset = 0f;

    private float _angle = 0f;
    private Vector3 _axis = Vector3.zero;

    private void Start()
    {
        _axis = Random.insideUnitSphere;
    }

    private void Update()
    {
        _angle += _speed * Time.deltaTime;

        _axis = Quaternion.Euler(Random.Range(1, _axisOffset), Random.Range(1f, _axisOffset), Random.Range(1f, _axisOffset)) * _axis;

        Vector3 offset = Quaternion.AngleAxis(_angle, _axis) * (Vector3.right * radius);
        transform.position = _center.position + offset;
    }
}
