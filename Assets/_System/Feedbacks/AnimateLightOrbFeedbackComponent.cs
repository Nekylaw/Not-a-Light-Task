using UnityEngine;
using Game.Services.LightSources;
using System.Collections.Generic;
using System.Collections;

public class AnimateLightOrbFeedbackComponent : MonoBehaviour
{

    [SerializeField]
    private GameObject _orbParticuleFeedbackPrefab = null;

    private LightSourceComponent _lightSource;

    private int _remainingOrbs = 0;
    public float _radius = 1f;
    public float _orbitSpeed = 5f;

    private List<GameObject> _orbParticuleList = new();
    private void Awake()
    {
        if (!TryGetComponent<LightSourceComponent>(out _lightSource))
            return;
    }

    private void OnEnable()
    {
        LightSourcesService.Instance.OnTriggerLight += HandleTriggerLight;
    }

    private void OnDisable()
    {
        LightSourcesService.Instance.OnTriggerLight -= HandleTriggerLight;
    }

    void Start()
    {
        _remainingOrbs = _lightSource.Settings.RequiredOrbs;

        for (int i = 0; i < _remainingOrbs; i++)
        {
            var orb = GameObject.Instantiate<GameObject>(_orbParticuleFeedbackPrefab);
            float angle = i * (360 / _remainingOrbs);

            orb.transform.localPosition =
              _lightSource.LightPoint + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * _radius,
                Mathf.Exp(Mathf.Sin(angle * Mathf.Deg2Rad)) * _radius,
                Mathf.Sin(angle * Mathf.Deg2Rad) * _radius
                );

            _orbParticuleList.Add(orb);
        }

    }

    void Update()
    {
        Animate();
    }

    private void Animate()
    {
        if (_lightSource == null || _orbParticuleList == null)
            return;

        foreach (GameObject orb in _orbParticuleList)
        {
            if (orb == null)
                continue;

            orb.transform.RotateAround(_lightSource.LightPoint, Vector3.up, _orbitSpeed * Time.deltaTime);
        }
    }

    private void HandleTriggerLight(LightSourceComponent light)
    {
        if (light != _lightSource)
            return;

        int lastIndex = _orbParticuleList.Count - 1;
        GameObject orb = _orbParticuleList[lastIndex];
        _orbParticuleList.RemoveAt(lastIndex); 

        if (orb != null)
        {
            var fadeOut = orb.GetComponent<OrbFadeOutFeedbackComponent>();
            if (fadeOut)
            {
                fadeOut.FadeOut();
                StartCoroutine(DestroyOrbAfterFade(orb, fadeOut.FadeDuration));
            }
            else
            {
                Destroy(orb);
            }
        }
    }

    private IEnumerator DestroyOrbAfterFade(GameObject orb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (orb != null)
            Destroy(orb);
    }

}
