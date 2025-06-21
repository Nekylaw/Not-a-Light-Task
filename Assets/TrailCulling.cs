using System;
using Game.Services.CullingService;
using UnityEngine;
using UnityEngine.VFX;

public class TrailCulling : MonoBehaviour,ICullable
{
    #region Cull

    public ICullable.ECullMode CullMode { get; set; }
    public float CullRange { get; set; }
    public int CullableIndex { get; set; }

    private VisualEffect VFX;
    private TrailRenderer TrailRenderer;

    private void OnEnable()
    {
        CullingService.Instance.Register(this);
    }

    private void OnDisable()
    {
        CullingService.Instance.Unregister(this);
    }

    private void Start()
    {
        CullMode = ICullable.ECullMode.Frustum;
        CullRange = 50f;
        VFX = GetComponentInChildren<VisualEffect>();
        TrailRenderer = GetComponentInChildren<TrailRenderer>();
    }

    private bool _isCulled;
    public void OnBecomeVisible()
    {
        _isCulled = false;
        VFX.enabled = true;
        TrailRenderer.enabled = true;
    }

    public void OnBecomeInvisible()
    {
        _isCulled = true;

        Debug.Log($"{name} has been culled");
        
        VFX.enabled = false;
        TrailRenderer.enabled = false;
    }

    public Vector3 GetCullPosition()
    {
        return transform.position;
    }

    public float GetCullRadius()
    {
        return CullRange;
    }

    #endregion
}
