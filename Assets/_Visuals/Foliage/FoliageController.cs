using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Game.Services.LightSources;
using System.Linq;

public class FoliageController : MonoBehaviour
{
    [SerializeField]
    private Material _material;

    [SerializeField]
    private int _chunkLoadRadius = 1;

    [SerializeField]
    private string _chunkFolder = "FoliageChunks";

    [SerializeField]
    private float _renderDistance = 100f;

    [SerializeField]
    private float _cullDistance = 150f;

    [Tooltip("Make sure the chunk size is the same with the foliage baker")]
    [SerializeField]
    private Vector2 _chunkSize = new Vector2(100f, 100f);


    private LightSourcesService _lightService = null;
    private Transform _player;

    private Dictionary<string, FoliageChunkInstance> _loadedChunks = new();
    private Vector2Int _currentPlayerChunk;

    private int _maxClearZonesCount = 0;
    private Vector4[] _clearZoneArray = { };

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>()?.transform;
        if (!_player)
        {
            Debug.LogError("No PlayerController found.");
            enabled = false;
            return;
        }

        _lightService = LightSourcesService.Instance;

        if (_lightService == null || _lightService.TotalLightSources <= 0)
            return;

        _maxClearZonesCount = _lightService.TotalLightSources;
        _clearZoneArray = new Vector4[_maxClearZonesCount];

        UpdatePlayerChunk();
    }

    private void Update()
    {
        if (!_player) return;

        UpdateClearZones();
        UpdatePlayerChunk();
        DrawVisibleChunks();
    }

    /// <summary>
    /// Updates the player's current chunk based on their position.
    /// </summary>
    /// <param name="forceUpdate"></param>
    private void UpdatePlayerChunk()
    {
        // Calculate the current chunk based on player's position.
        Vector2Int current = new Vector2Int(
            Mathf.FloorToInt(_player.position.x / _chunkSize.x),
            Mathf.FloorToInt(_player.position.z / _chunkSize.y)
        );

        if (current != _currentPlayerChunk)
        {
            _currentPlayerChunk = current;
            LoadChunksAround(current);
        }
    }

    private void LoadChunksAround(Vector2Int center)
    {
        var chunkToDraw = new HashSet<string>();

        for (int dx = -_chunkLoadRadius; dx <= _chunkLoadRadius; dx++)
        {
            for (int dz = -_chunkLoadRadius; dz <= _chunkLoadRadius; dz++)
            {
                Vector2Int chunkPos = new Vector2Int(center.x + dx, center.y + dz);
                string chunkKey = $"{chunkPos.x}_{chunkPos.y}";
                chunkToDraw.Add(chunkKey);

                if (!_loadedChunks.ContainsKey(chunkKey))
                    TryLoadChunk(chunkKey);
            }
        }

        var loadedChunks = new List<string>(_loadedChunks.Keys);

        // Unload far chunks
        foreach (var chunkId in loadedChunks)
        {
            if (!chunkToDraw.Contains(chunkId))
            {
                _loadedChunks[chunkId].Dispose();
                _loadedChunks.Remove(chunkId);
            }
        }
    }

    private void TryLoadChunk(string chunkKey)
    {
        string path = Path.Combine(_chunkFolder, chunkKey);
        TextAsset json = Resources.Load<TextAsset>(path);
        if (!json) return;

        ChunkData data = JsonUtility.FromJson<ChunkData>(json.text);
        if (data == null || data.MeshDatas == null) return;

        var chunkInstance = new FoliageChunkInstance(data, _material);
        _loadedChunks.Add(chunkKey, chunkInstance);
    }

    private void DrawVisibleChunks()
    {
        foreach (var chunk in _loadedChunks.Values)
            chunk.Draw(_player.position, _renderDistance, _cullDistance);
    }

    private void UpdateClearZones()
    {
        LightSourceComponent[] lightSources = _lightService.LightSources;

        for (int i = 0; i < _maxClearZonesCount; i++)
        {
            Vector3 pos = lightSources[i].transform.position;
            _clearZoneArray[i] = new Vector4(pos.x, pos.y, pos.z, lightSources[i].Settings.BrightnessRange);
        }

        _material.SetInt("_ClearZoneCount", lightSources.Where(s => s.IsLightOn == true).Count());
        _material.SetVectorArray("_ClearZones", _clearZoneArray);
    }
}
