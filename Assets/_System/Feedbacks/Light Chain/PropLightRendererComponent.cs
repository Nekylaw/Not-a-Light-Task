using Game.Services.LightSources;
using UnityEngine;

public class PropLightRendererComponent : MonoBehaviour
{
    [Header("Color")]
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color _glowColor = Color.white;

    [Header("Intensity")]
    [SerializeField] private float _baseIntensity = 1.0f;
    [SerializeField] private float _glowIntensity = 2.0f;

    [Header("Animation")]
    [SerializeField] private float _glowAnimSpeed = 2.0f;
    [SerializeField] private float _glowAnimAmplitude = 0.2f;

    [Header("Glow")]
    [SerializeField] private float _glowRadius = 0.2f;
    [SerializeField] private float _visibilityDistance = 20.0f;
    [SerializeField] private float _maxScreenDist = 0.5f;

    private Transform _player;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private LightSourceComponent _lightSource = null;

    private void Awake()
    {
        _player = FindFirstObjectByType<PlayerController>().transform;
        _renderer = GetComponent<Renderer>();
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

        if (_lightSource == null || !_lightSource.IsLightOn)
        {
            _mpb.SetFloat("_BaseIntensity", 0);
            _mpb.SetFloat("_GlowIntensity", 0);

            _renderer.SetPropertyBlock(_mpb);
            return;
        }

        _mpb.SetColor("_BaseColor", _baseColor);
        _mpb.SetColor("_GlowColor", _glowColor);
        _mpb.SetFloat("_BaseIntensity", _baseIntensity);
        _mpb.SetFloat("_GlowIntensity", _glowIntensity);
        _mpb.SetFloat("_GlowAnimationSpeed", _glowAnimSpeed);
        _mpb.SetFloat("_GlowAnimationAmplitude", _glowAnimAmplitude);
        _mpb.SetFloat("_GlowRadius", _glowRadius);
        _mpb.SetFloat("_GlowVisibilityThreshold", _visibilityDistance);
        _mpb.SetFloat("_MaxScreenDist", _maxScreenDist);
        _mpb.SetVector("_PlayerPosition", _player.position);
        _renderer.SetPropertyBlock(_mpb);
    }

}
