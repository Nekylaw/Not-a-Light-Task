using UnityEngine;

public class FlowerGrowRenderer : MonoBehaviour
{
    private Transform _player;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    private Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        _player = FindFirstObjectByType<PlayerController>().transform;

        _renderers = GetComponentsInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }


    private void Start()
    {
        foreach (var renderer in _renderers)
        {
            Vector3 baseScale = renderer.transform.lossyScale;
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetVector("_BaseScale", baseScale);
            renderer.SetPropertyBlock(_mpb);
        }
    }

    private void Update()
    {
        if (!_player) return;

        foreach (var renderer in _renderers)
        {
            renderer.GetPropertyBlock(_mpb);
            _mpb.SetVector("_PlayerPos", _player.position);
            renderer.SetPropertyBlock(_mpb);
        }
    }
}
