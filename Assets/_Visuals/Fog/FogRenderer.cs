using System;
using System.Collections;

using UnityEngine;

using Game.Services.LightSources;
using Game.Services.Fog;

public class FogRenderer : MonoBehaviour, IDisposable
{

    #region Singleton

    public static FogRenderer Instance { get; private set; }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _fogService = FogService.Instance;
        if (_fogService == null)
            Debug.LogError($"{nameof(FogService)} service not found.", this);


        Init();
        Debug.Log($"Init {nameof(FogRenderer)}");
    }

    #endregion


    #region Fields


    [SerializeField]
    private Material _fogMaterial = null;

    private FogService _fogService = null;

    private ComputeBuffer _clearZonesBuffer = null;

    private ClearZoneData[] _clearZonesDatas;

    private bool _disposed = false;

    private bool _initialized = false;



    #endregion


    #region Lifecycle

    private void Init()
    {
        if (_fogService == null || _fogService.MaxDissipationZones <= 0)
            return;

        _clearZonesDatas = new ClearZoneData[_fogService.MaxDissipationZones];
        _clearZonesBuffer = new ComputeBuffer(_fogService.MaxDissipationZones, sizeof(float) * 4);

        Debug.Log("Light sources count: " + _fogService.MaxDissipationZones);
        Debug.Log($"{nameof(FogRenderer)} Buffer created.");
    }

    private void OnEnable()
    {
        //@todo OnlightChange instead ?
        _fogService.OnFogDissipationStart += HandleLightOn;

        //@todo add fog delegate to update light if one is set to off
        _fogService.OnFogDissipationFinish += HandleLightOff;
    }

    private void OnDisable()
    {
        _fogService.OnFogDissipationStart -= HandleLightOn;
        _fogService.OnFogDissipationFinish -= HandleLightOff;

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

        int lightCount = _fogService.MaxDissipationZones;
        float animDuration = 5f; // @todo light on animation settings
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
                //_clearZonesDatas[index].Position = _fogService.LightSources[index].LightPoint;

                _clearZonesBuffer.SetData(_clearZonesDatas);
                _fogMaterial.SetInt("_ClearZoneCount", lightCount);
                _fogMaterial.SetBuffer("_ClearZones", _clearZonesBuffer);
            }

            Debug.Log("Defog");

            yield return null;
        }

        // Ensure apply effects
        _clearZonesDatas[index].Radius = endRadius;
        //_clearZonesDatas[index].Position = _fogService.LightSources[index].LightPoint;

        _clearZonesBuffer.SetData(_clearZonesDatas);
        _fogMaterial.SetInt("_ClearZoneCount", lightCount);
        _fogMaterial.SetBuffer("_ClearZones", _clearZonesBuffer);
    }


    private void HandleLightOn(LightSourceComponent light)
    {
        //int index = Array.IndexOf(_fogService.LightSources, light);

        //if (index < 0)
        //    return;

        //AnimateFogDissipation(index, 50f); // @todo max redius setting
    }

    private void HandleLightOff(LightSourceComponent light)
    {
        //int index = Array.IndexOf(_fogService.LightSources, light);
        //if (index >= 0)
        //    AnimateFogDissipation(index, 0f); // min radius setting
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
