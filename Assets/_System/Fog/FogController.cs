using System;

using UnityEngine;

using Game.Services.LightSources;

public class FogController : MonoBehaviour, IDisposable
{

    #region Buffer-structs

    private struct ClearZonePositionBufferData
    {
        public Vector3 Position;
        public float StartRadius;

        public ClearZonePositionBufferData(Vector3 position, float startRadius)
        {
            Position = position;
            StartRadius = startRadius;
        }
    }

    private struct ClearZoneAnimationBufferData
    {
        public float EndRadius;
        public float Speed;
        public float StartTime;

        public ClearZoneAnimationBufferData(float endRadius, float speed, float startTime)
        {
            EndRadius = endRadius;
            Speed = speed;
            StartTime = startTime;
        }
    }

    #endregion  


    #region Singleton

    public static FogController Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion


    #region Fields

    private const string SVClearZonesCount = "_ClearZoneCount";
    private const string SVClearZonesPositions = "_ClearZonesPositions";
    private const string SVClearZonesAnimations = "_ClearZonesAnimations";

    [SerializeField]
    private Material _fogMaterial = null;

    /// <summary>
    /// Light service reference. Handles delegates to update fog buffers.
    /// </summary>
    private LightSourcesService _lightService = null;

    /// <summary>
    /// Datas for each light sources to read to define clear zone postions.
    /// </summary>
    private ClearZonePositionBufferData[] _clearZonesPositionBufferDatas;

    /// <summary>
    /// Datas for each light sources to read to define how clear zone are animated.
    /// </summary>
    private ClearZoneAnimationBufferData[] _clearZonesAnimBufferDatas;

    /// <summary>
    /// Positions datas buffer. xyz = position, w = start radius.
    /// </summary>
    private ComputeBuffer _clearZonesPositionBuffer = null;

    /// <summary>
    /// Animations datas Buffer for. x = target, y = anim speed, z = start time
    /// </summary>
    private ComputeBuffer _clearZonesAnimBuffer = null;

    private bool _disposed = false;

    #endregion


    #region Lifecycle

    private void OnEnable()
    {
        LightSourcesService.Instance.OnSwitchOnLight += HandleLightOn;
        LightSourcesService.Instance.OnSwitchOffLight += HandleLightOff;

        //Init();
    }

    private void OnDisable()
    {
        LightSourcesService.Instance.OnSwitchOnLight -= HandleLightOn;
        LightSourcesService.Instance.OnSwitchOffLight -= HandleLightOff;

        ReleaseBuffer();
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        _lightService = LightSourcesService.Instance;

        if (_lightService == null || _lightService.TotalLightSources <= 0)
            return;

        int count = _lightService.TotalLightSources;

        _clearZonesPositionBufferDatas = new ClearZonePositionBufferData[count];
        _clearZonesAnimBufferDatas = new ClearZoneAnimationBufferData[count];

        _clearZonesPositionBuffer = new ComputeBuffer(count, sizeof(float) * 4);
        //Debug.Log($"{nameof(FogRenderer)} position Buffer created");
        _clearZonesAnimBuffer = new ComputeBuffer(count, sizeof(float) * 3);
        //Debug.Log($"{nameof(FogRenderer)} anim Buffer created");

        UpdateFogBuffers();
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

    private void HandleLightOn(LightSourceComponent light)
    {
        int index = Array.IndexOf(_lightService.LightSources, light);
        if (index < 0)
            return;

        //Debug.Log("Index " + index);
        //Debug.Log("Buffer size  " + _clearZonesPositionBufferDatas.Length);
        float currentRadius = _clearZonesPositionBufferDatas[index].StartRadius;
        _clearZonesPositionBufferDatas[index] = new ClearZonePositionBufferData(light.LightPoint, currentRadius);
        _clearZonesAnimBufferDatas[index] = new ClearZoneAnimationBufferData(light.Settings.BrightnessRange, Mathf.Max(1, light.Settings.DissipationSpeed), Time.time);

        UpdateFogBuffers();
    }

    private void HandleLightOff(LightSourceComponent light)
    {
        int index = Array.IndexOf(_lightService.LightSources, light);
        if (index < 0)
            return;

        _clearZonesAnimBufferDatas[index] = new ClearZoneAnimationBufferData(0, 5, Time.time); //@todo setup dissp spread speed

        UpdateFogBuffers();
    }

    private void UpdateFogBuffers()
    {
        _clearZonesPositionBuffer.SetData(_clearZonesPositionBufferDatas);
        _clearZonesAnimBuffer.SetData(_clearZonesAnimBufferDatas);

        _fogMaterial.SetInt(SVClearZonesCount, _lightService.TotalLightSources);

        _fogMaterial.SetBuffer(SVClearZonesPositions, _clearZonesPositionBuffer);
        _fogMaterial.SetBuffer(SVClearZonesAnimations, _clearZonesAnimBuffer);
    }

    private bool ReleaseBuffer()
    {
        bool released = false;

        if (_clearZonesPositionBuffer != null)
        {
            _clearZonesPositionBuffer.Release();
            _clearZonesPositionBuffer = null;
            released = true;
        }

        if (_clearZonesAnimBuffer != null)
        {
            _clearZonesAnimBuffer.Release();
            _clearZonesAnimBuffer = null;
            released = true;
        }

        return released;
    }


    #endregion

}
