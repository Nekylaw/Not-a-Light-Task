using Game.Services.LightSources;
using UnityEngine;

/// <summary>
/// Defines a component that manages the rendering of a prop light individually using a shared material.
/// </summary>
public class PropLightRendererComponent : MonoBehaviour
{
    [Header("Color")]
    [SerializeField] private bool _visibleIfLightOff = true;
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color _glowColor = Color.white;

    [Header("Intensity")]
    [SerializeField] private float _baseIntensity = 15f;
    [SerializeField] private float _glowIntensity = 20f;

    [Header("Animation")]
    [SerializeField] private float _glowAnimSpeed = 1f;
    [SerializeField] private float _glowAnimAmplitude = 10f;

    [Header("Glow")]
    [SerializeField] private float _glowRadius = 10f;
    [SerializeField] private float _visibilityDistance = 100f;
    [SerializeField] private float _maxScreenDist = 1f;

    [Header("Fade")]
    [SerializeField] private float _fadeSpeed = 3f;

    private float _currentBaseIntensity = 0f;
    private float _currentGlowIntensity = 0f;

    private Transform _player;
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private LightSourceComponent _lightSource;

    private void Awake()
    {
        var playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
            return;

        _player = playerController.transform;
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer == null)
            return;

        _mpb = new MaterialPropertyBlock();
        _lightSource = GetComponentInParent<LightSourceComponent>();
    }

    private void Update()
    {
        UpdateGlow();
    }

    private void OnValidate()
    {
        UpdateGlow();
    }

    private void UpdateGlow()
    {
        if (_renderer == null || _mpb == null || _player == null)
            return;

        _renderer.GetPropertyBlock(_mpb);

        bool isOn = _lightSource != null && _lightSource.IsLightOn;

        float targetBase = (isOn || _visibleIfLightOff) ? _baseIntensity : 0f;
        float targetGlow = (isOn || _visibleIfLightOff) ? _glowIntensity : 0f;

        _currentBaseIntensity = Mathf.Lerp(_currentBaseIntensity, targetBase, Time.deltaTime * _fadeSpeed);
        _currentGlowIntensity = Mathf.Lerp(_currentGlowIntensity, targetGlow, Time.deltaTime * _fadeSpeed);

        if (_mpb.HasProperty("_PropColor"))
            _mpb.SetColor("_PropColor", _baseColor);
        
        
        _mpb.SetColor("_GlowColor", _glowColor);
        _mpb.SetFloat("_BaseIntensity", _currentBaseIntensity);
        _mpb.SetFloat("_GlowIntensity", _currentGlowIntensity);
        _mpb.SetFloat("_GlowAnimationSpeed", _glowAnimSpeed);
        _mpb.SetFloat("_GlowAnimationAmplitude", _glowAnimAmplitude);
        _mpb.SetFloat("_GlowRadius", _glowRadius);
        _mpb.SetFloat("_GlowVisibilityThreshold", _visibilityDistance);
        _mpb.SetFloat("_MaxScreenDist", _maxScreenDist);
        _mpb.SetVector("_PlayerPosition", _player.position);

        _renderer.SetPropertyBlock(_mpb);
    }

    public void SetColor(Color baseColor, Color glowColor)
    {
        _baseColor = baseColor;
        _glowColor = glowColor;
    }
}
