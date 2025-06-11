using System;
using System.Collections;
using UnityEngine;

public class VortexEffectController : MonoBehaviour
{
    [SerializeField]
    private float _vortexRadius = 0.75f;

    [SerializeField]
    private float _vortexSpeed = 15f;
    
    [SerializeField]
    private float _vortexOutDuration = 0.5f;

    [SerializeField]
    private Material _vortexMaterial;

    private Coroutine _vortexCoroutine = null;

    private void Awake()
    {
        FindFirstObjectByType<PickUpBehaviorComponent>().OnHoldPickup += HandleHold;
        FindFirstObjectByType<PickUpBehaviorComponent>().OnReleasePickup += HandleRelease;
    }

    private void HandleHold()
    {
        if (_vortexMaterial == null)
            return;

        if (_vortexCoroutine != null)
            return;

        _vortexCoroutine = StartCoroutine(AnimateVortex(_vortexRadius));
    }

    private void HandleRelease()
    {
        _vortexCoroutine = null;
        _vortexCoroutine = StartCoroutine(AnimateVortex(0));
    }

    private IEnumerator AnimateVortex(float targetRadius)
    {
        if (_vortexMaterial == null)
        {
            _vortexCoroutine = null;
            yield break;
        }

        float duration = _vortexOutDuration;
        float elapsed = 0f;
        float initialRadius = _vortexMaterial.GetFloat("_Radius");
        float initialSpeed = _vortexMaterial.GetFloat("_TimeSpeed");

        if (initialRadius == targetRadius)
        {
            _vortexMaterial.SetFloat("_Radius", targetRadius);
            _vortexMaterial.SetFloat("_TimeSpeed", _vortexSpeed);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float radiusT = Mathf.Lerp(initialRadius, targetRadius, elapsed / duration);
            _vortexMaterial.SetFloat("_Radius", radiusT);

            float speedT = Mathf.Lerp(initialSpeed, _vortexSpeed, elapsed / duration);
            _vortexMaterial.SetFloat("_TimeSpeed",speedT);
            yield return null;
        }

        _vortexMaterial.SetFloat("_Radius", targetRadius);
        _vortexMaterial.SetFloat("_TimeSpeed", _vortexSpeed);

        _vortexCoroutine = null;
    }

}
