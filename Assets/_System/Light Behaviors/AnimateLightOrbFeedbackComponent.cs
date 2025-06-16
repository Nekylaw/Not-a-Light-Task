using UnityEngine;
using Game.Services.LightSources;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening; 

public class AnimateLightOrbFeedbackComponent : MonoBehaviour
{
    [SerializeField]
    private GameObject _orbParticuleFeedbackPrefab = null;

    private LightSourceComponent _lightSource;
    private int _remainingOrbs = 0;

    [Header("Orbit Settings")]
    public float _radius = 1f;
    public float _orbitSpeed = 5f;
    public float _verticalAmplitude = 0.15f;
    public float _verticalSpeed = 2f;

    [Header("Spawn Animation")]
    public float _spawnDelay = 0.1f;
    public float _spawnDuration = 0.5f;
    public Ease _spawnEase = Ease.OutBack;

    [Header("Fade Animation")]
    public float _fadeInDuration = 0.3f;
    public float _fadeOutDuration = 0.5f;

    public bool isLightOn = false;

    private List<GameObject> _orbParticuleList = new();
    private Coroutine _spawnCoroutine;

    private void Awake()
    {
        if (!TryGetComponent<LightSourceComponent>(out _lightSource))
            return;
    }

    private void OnEnable()
    {
        LightSourcesService.Instance.OnTriggerLight += HandleTriggerLight;
        LightSourcesService.Instance.OnSwitchOffLight += HandleLightOff;
    }

    private void OnDisable()
    {
        LightSourcesService.Instance.OnTriggerLight -= HandleTriggerLight;
        LightSourcesService.Instance.OnSwitchOffLight -= HandleLightOff;

        if (_spawnCoroutine != null)
            StopCoroutine(_spawnCoroutine);
    }

    void Start()
    {
        InitRequiredOrbs();
    }

    private void InitRequiredOrbs()
    {
        ClearExistingOrbs();

        _remainingOrbs = _lightSource.Settings.RequiredOrbs;
        isLightOn = false;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        _spawnCoroutine = StartCoroutine(SpawnOrbsWithAnimation());
    }

    private IEnumerator SpawnOrbsWithAnimation()
    {
        for (int i = 0; i < _remainingOrbs; i++)
        {
            var orb = GameObject.Instantiate<GameObject>(_orbParticuleFeedbackPrefab);
            orb.transform.SetParent(transform);

            float angle = i * (360f / _remainingOrbs);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * _radius,
                0,
                Mathf.Sin(angle * Mathf.Deg2Rad) * _radius
            );

            Vector3 targetPosition = _lightSource.LightPoint.position + offset;


            orb.transform.position = _lightSource.LightPoint.position;
            orb.transform.localScale = Vector3.zero;

            Sequence spawnSequence = DOTween.Sequence();

            spawnSequence.Append(
                orb.transform.DOScale(new Vector3(0.3f,0.3f,0.3f), _spawnDuration)
                    .SetEase(_spawnEase)
            );

            spawnSequence.Join(
                orb.transform.DOMove(targetPosition, _spawnDuration)
                    .SetEase(Ease.OutCubic)
            );

            spawnSequence.Join(
                orb.transform.DORotate(new Vector3(0, 360, 0), _spawnDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutQuad)
            );

            _orbParticuleList.Add(orb);

            yield return new WaitForSeconds(_spawnDelay);
        }

        _spawnCoroutine = null;
    }

    private void ClearExistingOrbs()
    {
        foreach (GameObject orb in _orbParticuleList)
        {
            if (orb != null)
            {
                orb.transform.DOKill();
                Destroy(orb);
            }
        }
        _orbParticuleList.Clear();
    }

    void Update()
    {
        if (!isLightOn && _spawnCoroutine == null) 
        {
            AnimateOrbits();
        }
    }

    private void AnimateOrbits()
    {
        if (_lightSource == null || _orbParticuleList == null || _orbParticuleList.Count == 0)
            return;

        for (int i = 0; i < _orbParticuleList.Count; i++)
        {
            GameObject orb = _orbParticuleList[i];
            if (orb == null)
                continue;

            float phaseOffset = (360f / _orbParticuleList.Count) * i;
            float currentAngle = (Time.time * _orbitSpeed + phaseOffset) * Mathf.Deg2Rad;

            float verticalOffset = Mathf.Sin(Time.time * _verticalSpeed + i) * _verticalAmplitude;

            Vector3 offset = new Vector3(
                Mathf.Cos(currentAngle) * _radius,
                verticalOffset,
                Mathf.Sin(currentAngle) * _radius
            );

            orb.transform.position = _lightSource.LightPoint.position + offset;

            orb.transform.Rotate(Vector3.up, _orbitSpeed * 2f * Time.deltaTime);
            orb.transform.Rotate(Vector3.right, _orbitSpeed * 0.5f * Time.deltaTime);
        }
    }

    private void HandleTriggerLight(LightSourceComponent light)
    {
        if (light != _lightSource || _orbParticuleList.Count == 0)
            return;

        int lastIndex = _orbParticuleList.Count - 1;
        GameObject orb = _orbParticuleList[lastIndex];
        _orbParticuleList.RemoveAt(lastIndex);

        if (_orbParticuleList.Count == 0)
        {
            isLightOn = true;
        }

        if (orb != null)
        {
            ConsumeOrbWithAnimation(orb);
        }
    }

    private void ConsumeOrbWithAnimation(GameObject orb)
    {
        orb.transform.DOKill();

        Sequence consumeSequence = DOTween.Sequence();

        consumeSequence.Append(
            orb.transform.DOMove(_lightSource.LightPoint.position, _fadeOutDuration)
                .SetEase(Ease.InCubic)
        );

        consumeSequence.Join(
            orb.transform.DOScale(0f, _fadeOutDuration)
                .SetEase(Ease.InBack)
        );

        consumeSequence.Join(
            orb.transform.DORotate(new Vector3(0, 720, 360), _fadeOutDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.InQuad)
        );

        consumeSequence.OnComplete(() => {
            if (orb != null)
                Destroy(orb);
        });

        var fadeOut = orb.GetComponent<OrbFadeOutFeedbackComponent>();
        if (fadeOut)
        {
            fadeOut.FadeOut();
        }
    }

    private void HandleLightOff(LightSourceComponent light)
    {
        if (light != _lightSource)
            return;

        StartCoroutine(DelayedRespawn());
    }

    private IEnumerator DelayedRespawn()
    {
        yield return new WaitForSeconds(0.3f);
        InitRequiredOrbs();
    }

    public void ResetOrbDisplay()
    {
        InitRequiredOrbs();
    }

    void OnDestroy()
    {
        DOTween.Kill(transform);
        ClearExistingOrbs();
    }

    public void DisturbOrbs(float intensity = 1f)
    {
        foreach (var orb in _orbParticuleList)
        {
            if (orb != null)
            {
                orb.transform.DOShakePosition(0.3f, 0.1f * intensity, 10, 90, false, true)
                    .SetEase(Ease.OutQuad);

                orb.transform.DOPunchScale(Vector3.one * 0.2f * intensity, 0.3f, 5, 0.5f);
            }
        }
    }
}