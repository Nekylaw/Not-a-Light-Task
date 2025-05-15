using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class OrbFadeOutFeedbackComponent : MonoBehaviour
{
    [SerializeField]
    private float _fadeDuration = 1.0f;

    private MaterialPropertyBlock _mpb;
    private Renderer _renderer;
    private float _currentVisibility;
    private bool _isFading = false;
    private float _fadeTimer = 0f;

    private const string GlowVisibilityThreshold = "_GlowVisibilityThreshold";

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_mpb);
        _currentVisibility = _mpb.GetFloat(GlowVisibilityThreshold);
    }

    private void Update()
    {
        if (!_isFading)
            return;

        _fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_fadeTimer / _fadeDuration);
        float newVisibility = Mathf.Lerp(_currentVisibility, 0f, t);

        _mpb.SetFloat(GlowVisibilityThreshold, newVisibility);
        _renderer.SetPropertyBlock(_mpb);

        if (t >= 1f)
            _isFading = false;
    }

    public bool IsFading => _isFading;
    public float FadeDuration => _fadeDuration;

    public void FadeOut()
    {
        if (_isFading)
            return;

        _renderer.GetPropertyBlock(_mpb);
        _currentVisibility = _mpb.GetFloat(GlowVisibilityThreshold);
        _fadeTimer = 0f;
        _isFading = true;
    }
}
