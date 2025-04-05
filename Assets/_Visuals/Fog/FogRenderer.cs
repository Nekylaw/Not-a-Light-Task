using System;
using System.Collections;
using UnityEditor.Callbacks;
using UnityEngine;

public class FogRenderer : MonoBehaviour, IDisposable
{

    #region Singleton

    public static FogRenderer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _lightService = LightSourcesService.Instance;
        if (_lightService == null)
            Debug.LogError($"Error, {nameof(LightSourcesService)} not found.", this);

        Debug.Log($"Init {nameof(FogRenderer)}");

    }

    #endregion


    #region Nested

    struct ClearZoneData
    {
        public Vector3 Position;
        public float Radius;
    }

    #endregion


    #region Fields

    [SerializeField]
    private Material _fogMaterial = null;

    private LightSourcesService _lightService = null;

    private ComputeBuffer _clearZonesBuffer = null;

    private ClearZoneData[] _clearZonesDatas;

    private bool _disposed = false;

    #endregion


    #region Lifecycle

    private void Start()
    {
        if (_lightService == null || _lightService.LightSourceCount <= 0)
            return;

        _clearZonesDatas = new ClearZoneData[_lightService.LightSourceCount];
        _clearZonesBuffer = new ComputeBuffer(_lightService.LightSourceCount, sizeof(float) * 4);

        Debug.Log("Light sources count: " +  _lightService.LightSourceCount);

        Debug.Log($"Init {nameof(FogRenderer)} Buffer");

    }

    private void OnEnable()
    {
        if (_lightService == null)
            return;

        //OnlightChange instead
        _lightService.OnSwitchOnLight += HandleLightOn;
        _lightService.OnSwitchOffLight += HandleLightOff;
    }

    private void OnDisable()
    {
        _lightService.OnSwitchOnLight -= HandleLightOn;
        _lightService.OnSwitchOffLight -= HandleLightOff;

        ReleaseBuffer();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            ReleaseBuffer();
            _disposed = true;
        }
    }

    #endregion


    #region Private API

    private void AnimateFogDissipation(int index, float endRadius)
    {
        StartCoroutine(AnimateFogDissipationCoroutine(index, endRadius));
    }

    private IEnumerator AnimateFogDissipationCoroutine(int index, float endRadius)
    {
            Debug.Log("Start Defog");

        int lightCount = _lightService.LightSourceCount;
        float animDuration = 3f; // @todo light on animation settings
        float elapsedTime = 0f;
        float startRadius = _clearZonesDatas[index].Radius;

        while (elapsedTime < animDuration)
        {
            elapsedTime += Time.deltaTime;
            float ratio = Mathf.SmoothStep(0f, 1f, elapsedTime / animDuration); // Ease-in/out pour une animation fluide

            float stepRadius = Mathf.Lerp(startRadius, endRadius, ratio);

            // Pass frame if not necessary to set buffer
            if (Mathf.Abs(stepRadius - _clearZonesDatas[index].Radius) > 0.01f) // @todo settings _stepRadiusThreshold
            {
                _clearZonesDatas[index].Radius = stepRadius;
                _clearZonesDatas[index].Position = _lightService.LightSources[index].LightPoint;

                _clearZonesBuffer.SetData(_clearZonesDatas);
                _fogMaterial.SetInt("_ClearZoneCount", lightCount);
                _fogMaterial.SetBuffer("_ClearZones", _clearZonesBuffer);
            }

            Debug.Log("Defog");

            yield return null;
        }

        // Ensure apply effects
        _clearZonesDatas[index].Radius = endRadius;
        _clearZonesDatas[index].Position = _lightService.LightSources[index].LightPoint;

        _clearZonesBuffer.SetData(_clearZonesDatas);
        _fogMaterial.SetInt("_ClearZoneCount", lightCount);
        _fogMaterial.SetBuffer("_ClearZones", _clearZonesBuffer);
    }


    private void HandleLightOn(LightSourceComponent light)
    {
        int index = Array.IndexOf(_lightService.LightSources, light);

        if (index < 0)
            return;

        AnimateFogDissipation(index, 10f); // @todo max redius setting
    }

    private void HandleLightOff(LightSourceComponent light)
    {
        int index = Array.IndexOf(_lightService.LightSources, light);
        if (index >= 0)
            AnimateFogDissipation(index, 0f); // min radius setting
    }

    private bool ReleaseBuffer()
    {
        if (_clearZonesBuffer != null)
        {
            _clearZonesBuffer.Release();
            _clearZonesBuffer = null;
            return true;
        }

        return false;
    }

    #endregion

}
