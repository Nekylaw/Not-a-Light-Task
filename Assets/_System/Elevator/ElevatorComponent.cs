using System.Collections;
using UnityEngine;

public class ElevatorComponent : MonoBehaviour
{
    [SerializeField]
    private Transform _topPoint = null;
    [SerializeField]
    private Transform _bottomPoint = null;
    [SerializeField]
    private float _duration = 2f;

    [SerializeField]
    private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool _isActivated = false;
    private bool _isTop = false;

    private Vector3 _bottomPos = Vector3.zero;
    private Vector3 _topPos = Vector3.zero;

    private void Awake()
    {
        transform.position = _bottomPoint.position;
        _isActivated = false;
        _isTop = false;

        _bottomPos = _bottomPoint.position;
        _topPos = _topPoint.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isActivated || !other.gameObject.TryGetComponent<PlayerController>(out var player))
            return;

        if (player.transform.parent != transform)
            player.transform.SetParent(transform);

        ActivateElevator();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent<PlayerController>(out var player))
            return;

        if (player.transform.parent == transform)
            player.transform.SetParent(null);
    }

    private void ActivateElevator()
    {
        if (_isActivated)
            return;

        _isActivated = true;
        Transform target = _isTop ? _bottomPoint : _topPoint;
        StartCoroutine(AnimateElevatorCoroutine(target));
    }

    private IEnumerator AnimateElevatorCoroutine(Transform target)
    {
        float elapsedTime = 0f;

        Vector3 targetPos = target == _topPoint ? _topPos : _bottomPos; 
        Vector3 startPos = transform.position;

        while (Vector3.Distance(transform.position, targetPos) > 0.1f)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / _duration);

            transform.position = Vector3.LerpUnclamped(startPos, targetPos, _animationCurve.Evaluate(t));
            yield return null;
        }

        transform.position = targetPos;
        _isTop = !_isTop;
        _isActivated = false;

        Debug.Log("Elevator reached target position.");
    }
}