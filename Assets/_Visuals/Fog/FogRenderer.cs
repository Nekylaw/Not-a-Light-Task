using System;
using System.Collections.Generic;
using UnityEngine;

public class FogRenderer : MonoBehaviour
{
    struct ClearZoneData
    {
        public Vector3 Position;
        public float Radius;
    }

    [SerializeField]
    private Material _fogMaterial;

    private ComputeBuffer _clearZonesBuffer;

    private ClearZoneData[] _clearZonesDatas;
    private const int MaxZones = 100;

    private LightSourcesService _lightService = null;


    private void Awake()
    {
        _lightService = FindFirstObjectByType<LightSourcesService>();

        if (_lightService == null)
            Debug.LogWarning("Error, fogRenderer not found.", this);
    }

    void Start()
    {
        _clearZonesDatas = new ClearZoneData[_lightService.LightOnCount];
        _clearZonesBuffer = new ComputeBuffer(_lightService.LightOnCount, sizeof(float) * 4);
        _fogMaterial.SetInt("_ClearZoneCount", 0);

        Render();
    }

    private void OnEnable()
    {
        _lightService.OnSwitchOnLight += HandleLightOn;
        _lightService.OnSwitchOffLight += HandleLightOff;
    }

    private void OnDisable()
    {
        _lightService.OnSwitchOnLight -= HandleLightOn;
        _lightService.OnSwitchOffLight -= HandleLightOff;
    }

    private void Render()
    {
        int lightCount = _lightService.LightOnCount;

        if (lightCount <= 0)
        {
            _fogMaterial.SetInt("_ClearZoneCount", 0);
            return;
        }

        if (_clearZonesBuffer == null || _clearZonesDatas.Length != lightCount)
        {
            if (_clearZonesBuffer != null)
                _clearZonesBuffer.Release();

            _clearZonesDatas = new ClearZoneData[lightCount];
            _clearZonesBuffer = new ComputeBuffer(lightCount, sizeof(float) * 4);
        }

        for (int i = 0; i < lightCount; i++)
        {
            _clearZonesDatas[i].Position = _lightService.LightSourceList[i].transform.position;
            _clearZonesDatas[i].Radius = 10f;
        }

        _clearZonesBuffer.SetData(_clearZonesDatas);
        _fogMaterial.SetInt("_ClearZoneCount", lightCount);
        _fogMaterial.SetBuffer("_ClearZones", _clearZonesBuffer);

        Debug.Log("Fog updated with " + lightCount + " light sources.");
    }

    private void HandleLightOn(LightSourceComponent light)
    {
        Render();
        Debug.Log("Fog register source");
    }

    private void HandleLightOff(LightSourceComponent light)
    {
        Render();
        Debug.Log("Fog remove source");
    }

    void OnDestroy()
    {
        if (_clearZonesBuffer != null)
        {
            _clearZonesBuffer.Release();
            _clearZonesBuffer = null;
        }
    }
}
