using System;
using Unity.VisualScripting;
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

        Debug.Log("Fog render''-----------");

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

    private LightSourcesService _lightService = null;

    [SerializeField]
    private Material _fogMaterial = null;

    private ComputeBuffer _clearZonesBuffer = null;

    private ClearZoneData[] _clearZonesDatas;

    private bool _disposed = false;

    #endregion


    #region Lifecycle

    private void Start()
    {
        if (_lightService == null)
            Debug.Log("Service null!", this);
        else
            Debug.Log("Service found!", this);

        if (_lightService == null || _lightService.LightSourceCount <= 0)
            return;

        _clearZonesDatas = new ClearZoneData[_lightService.LightSourceCount];
        _clearZonesBuffer = new ComputeBuffer(_lightService.LightSourceCount, sizeof(float) * 4);
        _fogMaterial.SetInt("_ClearZoneCount", 0);

        Render();
    }

    private void OnEnable()
    {
        //_lightService = LightSourcesService.Instance;


        if (_lightService == null)
            Debug.Log("Fog Eneable Service null!", this);

        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight += HandleLightOn;
            _lightService.OnSwitchOffLight += HandleLightOff;
        }
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

    private void Render()
    {
        int lightCount = _lightService.LightSourceCount;
        Debug.Log("Light on: " + lightCount);


        if (lightCount <= 0)
        {
            _fogMaterial.SetInt("_ClearZoneCount", 0);
            return;
        }

        for (int i = 0; i < lightCount; i++)
        {
            _clearZonesDatas[i].Position = _lightService.LightSourceList[i].transform.position;
            _clearZonesDatas[i].Radius = _lightService.LightSourceList[i].IsLightOn ? 10f : 0f; //@todo set radius to light asset brightness radius.
        }

        _clearZonesBuffer.SetData(_clearZonesDatas);
        _fogMaterial.SetInt("_ClearZoneCount", lightCount);
        _fogMaterial.SetBuffer("_ClearZones", _clearZonesBuffer);

        Debug.Log("Fog updated with " + lightCount + " light sources.");
    }

    private void HandleLightOn(LightSourceComponent light)
    {
        Debug.Log("Light on Invoked on fog");
        Render();
    }

    private void HandleLightOff(LightSourceComponent light)
    {
        Render();
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


    #region Public API
    #endregion

}
