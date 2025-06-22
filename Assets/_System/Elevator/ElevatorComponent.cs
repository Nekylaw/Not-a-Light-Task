using System.Collections;
using Game.Services.LightSources;
using Unity.Mathematics;
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

    //[SerializeField] private LightSourceComponent activator;

    [SerializeField]
    private OrbComponent orb;

    //[SerializeField] private Transform rejectPosition;
    //[SerializeField] private float RejectForce;
    
    private bool _isActivated = false;
    private bool _isTop = false;
    
    public bool isPlayerIn = false;

    private Vector3 _bottomPos = Vector3.zero;
    private Vector3 _topPos = Vector3.zero;

    private void OnEnable()
    {
        //LightSourcesService.Instance.OnSwitchOnLight += ActivateElevator;
    }

    private void OnDisable()
    {
        //LightSourcesService.Instance.OnSwitchOnLight -= ActivateElevator;
    }

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

        isPlayerIn = true;
        
        ActivateElevator();
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.TryGetComponent<PlayerController>(out var player))
            return;

        if (player.transform.parent == transform)
            player.transform.SetParent(null);
        
        isPlayerIn = false;
    }

    public void ActivateElevator()//LightSourceComponent light)
    {
        //if (light != activator)
          //  return;
        
        if (!isPlayerIn)
        {
            //RejectorBehaviour();
            //LightSourcesService.Instance.SwitchOff(light);
            return;
        }
        
        if (_isActivated)
            return;

        _isActivated = true;
        Transform target = _isTop ? _bottomPoint : _topPoint;
        StartCoroutine(AnimateElevatorCoroutine(target));
    }

    /*public void RejectorBehaviour()
    {
        var iObj = Instantiate( orb.gameObject, rejectPosition.position, quaternion.identity);
        //iObj.gameObject.GetComponent<Rigidbody>().AddForce( iObj.gameObject.transform.forward * RejectForce, ForceMode.Impulse);
    }*/

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

        //LightSourcesService.Instance.SwitchOff(activator);
        //RejectorBehaviour();
    }
}