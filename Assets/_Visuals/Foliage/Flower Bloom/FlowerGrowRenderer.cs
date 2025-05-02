using UnityEngine;

public class FlowerGrowRenderer : MonoBehaviour
{
    private Transform _player;
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private Vector3 _baseScale = Vector3.one;

    void Awake()
    {
        _player = FindFirstObjectByType<PlayerController>().transform;

        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }


    private void Start()
    {
        _baseScale = transform.lossyScale;
    }

    void Update()
    {
        if (!_player) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetVector("_PlayerPos", _player.position);
        _mpb.SetVector("_BaseScale", _baseScale);
        _renderer.SetPropertyBlock(_mpb);
    }
}
