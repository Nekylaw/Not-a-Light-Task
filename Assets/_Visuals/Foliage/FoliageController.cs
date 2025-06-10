using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Game.Services.LightSources;
using System.Linq;
using System.Collections;

/// <summary>
/// Controls foliage rendering with chunk-based loading, LOD system, and dynamic light-based clearing zones.
/// Manages performance through frustum culling, async loading, and frame budget management.
/// </summary>
public class FoliageController : MonoBehaviour
{
    [Header("Rendering Settings")]
    [SerializeField] private Material _material;
    [SerializeField] private float _renderDistance = 100f;
    [SerializeField] private float _cullDistance = 150f;
    [SerializeField] private float[] _lodDistances = { 50f, 100f, 200f, 500f };
    [SerializeField] private bool _enableLOD = true;
    [SerializeField] private bool _enableFrustumCulling = true;

    [Header("Chunk Loading")]
    [SerializeField] private int _chunkLoadRadius = 1;
    [SerializeField] private string _chunkFolder = "FoliageChunks";
    [SerializeField] private int _maxConcurrentLoads = 2;
    [SerializeField] private float _loadFrameBudgetMs = 5f;

    [Header("Chunk Configuration")]
    [Tooltip("Make sure the chunk size matches the foliage baker configuration")]
    [SerializeField] private Vector2 _chunkSize = new Vector2(100f, 100f);

    [Header("Performance")]
    [SerializeField] private int _maxChunksDrawnPerFrame = 50;
    [SerializeField] private bool _enableOcclusion = false;

    // References
    private LightSourcesService _lightService = null;
    private Transform _player;
    private Camera _playerCamera;

    // Chunk management
    private Dictionary<string, FoliageChunkInstance> _loadedChunks = new();
    private Dictionary<string, ChunkLoadOperation> _loadingChunks = new();
    private Vector2Int _currentPlayerChunk = Vector2Int.zero;
    private Vector2Int _lastPlayerChunk = Vector2Int.one * int.MaxValue; // Force initial load

    // Light-based clear zones for dynamic foliage scaling
    private int _maxClearZonesCount = 0;
    private Vector4[] _clearZoneArray = { };
    private bool _clearZonesChanged = true;

    // Async loading system
    private Queue<ChunkLoadOperation> _chunkLoadQueue = new();
    private bool _isLoadingChunks = false;
    private int _currentConcurrentLoads = 0;

    // Performance optimization caches
    private readonly HashSet<string> _visibleChunkKeys = new HashSet<string>();
    private readonly List<string> _chunksToUnload = new List<string>();
    private float _lastChunkUpdateTime = 0f;
    private const float CHUNK_UPDATE_INTERVAL = 0.1f; // Update chunks 10 times per second

    // Shader property IDs for performance
    private static readonly int ClearZoneCountId = Shader.PropertyToID("_ClearZoneCount");
    private static readonly int ClearZonesId = Shader.PropertyToID("_ClearZones");
    private ComputeBuffer _clearZonesBuffer;

    /// <summary>
    /// Represents a chunk loading operation with priority and timing information
    /// </summary>
    private struct ChunkLoadOperation
    {
        public string chunkKey;
        public Vector2Int chunkPos;
        public float priority; // Based on distance to player
        public System.DateTime startTime;
    }

    #region Unity Lifecycle

    private void Start()
    {
        InitializeReferences();
        InitializeLightSystem();

        // Force initial clear zones update
        _clearZonesChanged = true;

        if (_player)
        {
            UpdatePlayerChunk(true); // Force initial chunk load
        }
    }

    private void Update()
    {
        if (!_player) return;

        UpdateClearZones();

        // Update chunks at fixed intervals to avoid performance spikes
        if (Time.time - _lastChunkUpdateTime >= CHUNK_UPDATE_INTERVAL)
        {
            UpdatePlayerChunk();
            _lastChunkUpdateTime = Time.time;
        }

        // Render all visible chunks within performance budget
        DrawVisibleChunks();

        // Start async loading if queue has pending operations
        if (!_isLoadingChunks && _chunkLoadQueue.Count > 0 && _currentConcurrentLoads < _maxConcurrentLoads)
        {
            StartCoroutine(LoadChunkAsync());
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromLightEvents();
        DisposeAllChunks();
    }

    private void OnDestroy()
    {
        UnsubscribeFromLightEvents();
        DisposeAllChunks();
        _clearZonesBuffer?.Release();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize player and camera references required for chunk loading and culling
    /// </summary>
    private void InitializeReferences()
    {
        // Find player controller for chunk center calculation
        _player = FindFirstObjectByType<PlayerController>()?.transform;
        if (!_player)
        {
            Debug.LogError("FoliageController: No PlayerController found. Component disabled.");
            enabled = false;
            return;
        }

        // Find camera for frustum culling
        _playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        if (!_playerCamera)
        {
            Debug.LogWarning("FoliageController: No main camera found, frustum culling disabled.");
            _enableFrustumCulling = false;
        }

        // Initialize light service reference
        _lightService = LightSourcesService.Instance;
    }

    /// <summary>
    /// Initialize the light-based clearing system for dynamic foliage effects
    /// </summary>
    private void InitializeLightSystem()
    {
        if (_lightService == null)
        {
            Debug.LogWarning("FoliageController: LightSourcesService.Instance is null.");
            return;
        }

        if (_lightService.TotalLightSources <= 0)
        {
            Debug.LogWarning("FoliageController: No light sources found.");
            return;
        }

        // Initialize clear zones array and compute buffer
        _maxClearZonesCount = _lightService.TotalLightSources;
        _clearZoneArray = new Vector4[_maxClearZonesCount];
        _clearZonesBuffer = new ComputeBuffer(_maxClearZonesCount, sizeof(float) * 4);

        // Subscribe to light state change events
        _lightService.OnSwitchOnLight += OnLightChanged;
        _lightService.OnSwitchOffLight += OnLightChanged;

        Debug.Log($"FoliageController: Light system initialized with {_maxClearZonesCount} max zones");
    }

    #endregion

    #region Chunk Management

    /// <summary>
    /// Update current player chunk position and trigger chunk loading if needed
    /// </summary>
    private void UpdatePlayerChunk(bool forceUpdate = false)
    {
        Vector2Int current = new Vector2Int(
            Mathf.FloorToInt(_player.position.x / _chunkSize.x),
            Mathf.FloorToInt(_player.position.z / _chunkSize.y)
        );

        if (current != _currentPlayerChunk || forceUpdate)
        {
            _lastPlayerChunk = _currentPlayerChunk;
            _currentPlayerChunk = current;
            LoadChunksAround(current);
        }
    }

    /// <summary>
    /// Queue chunks for loading around the specified center position
    /// </summary>
    private void LoadChunksAround(Vector2Int center)
    {
        _visibleChunkKeys.Clear();
        var newLoadOperations = new List<ChunkLoadOperation>();

        // Generate load operations for chunks in radius
        for (int dx = -_chunkLoadRadius; dx <= _chunkLoadRadius; dx++)
        {
            for (int dz = -_chunkLoadRadius; dz <= _chunkLoadRadius; dz++)
            {
                Vector2Int chunkPos = new Vector2Int(center.x + dx, center.y + dz);
                string chunkKey = $"{chunkPos.x}_{chunkPos.y}";
                _visibleChunkKeys.Add(chunkKey);

                float distance = new Vector2(dx, dz).magnitude;

                // Only queue chunks that aren't already loaded or loading
                if (!_loadedChunks.ContainsKey(chunkKey) && !_loadingChunks.ContainsKey(chunkKey))
                {
                    newLoadOperations.Add(new ChunkLoadOperation
                    {
                        chunkKey = chunkKey,
                        chunkPos = chunkPos,
                        priority = distance, // Closer chunks have lower priority values
                        startTime = System.DateTime.Now
                    });
                }
            }
        }

        // Sort by priority (distance) and enqueue closest chunks first
        newLoadOperations.Sort((a, b) => a.priority.CompareTo(b.priority));
        foreach (var op in newLoadOperations)
        {
            _chunkLoadQueue.Enqueue(op);
            _loadingChunks[op.chunkKey] = op;
        }

        UnloadDistantChunks();
    }

    /// <summary>
    /// Unload chunks that are no longer within the visible radius
    /// </summary>
    private void UnloadDistantChunks()
    {
        _chunksToUnload.Clear();

        // Find chunks that are no longer visible
        foreach (var chunkId in _loadedChunks.Keys)
        {
            if (!_visibleChunkKeys.Contains(chunkId))
            {
                _chunksToUnload.Add(chunkId);
            }
        }

        // Dispose and remove distant chunks
        foreach (var chunkId in _chunksToUnload)
        {
            _loadedChunks[chunkId].Dispose();
            _loadedChunks.Remove(chunkId);
        }

        // Cancel loading operations for chunks that are no longer visible
        var loadingToCancel = _loadingChunks.Where(kvp => !_visibleChunkKeys.Contains(kvp.Key)).ToList();
        foreach (var kvp in loadingToCancel)
        {
            _loadingChunks.Remove(kvp.Key);
        }
    }

    /// <summary>
    /// Attempt to load a single chunk from Resources folder
    /// </summary>
    private void TryLoadChunk(string chunkKey)
    {
        string path = Path.Combine(_chunkFolder, chunkKey);
        TextAsset json = Resources.Load<TextAsset>(path);

        if (!json)
        {
            Debug.LogWarning($"FoliageController: Chunk file not found: {path}");
            return;
        }

        try
        {
            ChunkData data = JsonUtility.FromJson<ChunkData>(json.text);
            if (data?.MeshDatas == null)
            {
                Debug.LogWarning($"FoliageController: Invalid chunk data: {chunkKey}");
                return;
            }

            var chunkInstance = new FoliageChunkInstance(data, _material);
            _loadedChunks.Add(chunkKey, chunkInstance);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FoliageController: Error loading chunk {chunkKey}: {e.Message}");
        }
    }

    /// <summary>
    /// Async coroutine to load chunks while respecting frame budget
    /// </summary>
    private IEnumerator LoadChunkAsync()
    {
        _isLoadingChunks = true;
        _currentConcurrentLoads++;

        float frameStartTime = Time.realtimeSinceStartup * 1000f;

        while (_chunkLoadQueue.Count > 0)
        {
            // Check frame budget and yield if exceeded
            float currentTime = Time.realtimeSinceStartup * 1000f;
            if (currentTime - frameStartTime > _loadFrameBudgetMs)
            {
                yield return null;
                frameStartTime = Time.realtimeSinceStartup * 1000f;
            }

            var loadOp = _chunkLoadQueue.Dequeue();

            // Skip chunks that are no longer visible
            if (!_visibleChunkKeys.Contains(loadOp.chunkKey))
            {
                _loadingChunks.Remove(loadOp.chunkKey);
                continue;
            }

            // Load chunk if not already loaded
            if (!_loadedChunks.ContainsKey(loadOp.chunkKey))
            {
                TryLoadChunk(loadOp.chunkKey);
            }

            _loadingChunks.Remove(loadOp.chunkKey);
        }

        _currentConcurrentLoads--;
        _isLoadingChunks = false;
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Draw all loaded chunks within render distance using LOD and culling
    /// </summary>
    private void DrawVisibleChunks()
    {
        if (_loadedChunks.Count <= 0) return;

        int chunksDrawn = 0;
        Vector3 playerPos = _player.position;
        Camera camera = _enableFrustumCulling ? _playerCamera : null;

        foreach (var kvp in _loadedChunks)
        {
            // Respect frame budget for chunk drawing
            if (chunksDrawn >= _maxChunksDrawnPerFrame) break;

            var chunk = kvp.Value;

            // Distance culling - skip chunks beyond cull distance plus chunk radius
            if (Vector3.Distance(playerPos, chunk.ChunkCenter) > _cullDistance + chunk.ChunkRadius)
                continue;

            // Render with LOD or standard rendering based on settings
            if (_enableLOD)
            {
                chunk.DrawWithLOD(playerPos, _lodDistances, _cullDistance, camera);
            }
            else
            {
                chunk.Draw(playerPos, _renderDistance, _cullDistance, camera);
            }

            chunksDrawn++;
        }
    }

    #endregion

    #region Light-Based Clear Zones

    /// <summary>
    /// Update clear zones array based on active light sources for dynamic foliage effects
    /// </summary>
    private void UpdateClearZones()
    {
        if (_lightService == null || _maxClearZonesCount <= 0)
        {
            return;
        }

        LightSourceComponent[] lightSources = _lightService.LightSources;
        if (lightSources == null)
        {
            Debug.LogWarning("FoliageController: Light sources array is null");
            return;
        }

        bool hasChanges = false;
        int activeLightCount = 0;

        // Reset clear zones array and detect changes
        for (int i = 0; i < _clearZoneArray.Length; i++)
        {
            Vector4 oldValue = _clearZoneArray[i];
            _clearZoneArray[i] = Vector4.zero;
            if (oldValue != Vector4.zero)
                hasChanges = true;
        }

        // Populate active light zones
        for (int i = 0; i < lightSources.Length && activeLightCount < _maxClearZonesCount; i++)
        {
            if (lightSources[i] == null || !lightSources[i].IsLightOn)
                continue;

            Vector3 pos = lightSources[i].transform.position;
            float range = lightSources[i].Settings.BrightnessRange;
            Vector4 zone = new Vector4(pos.x, pos.y, pos.z, range);

            if (_clearZoneArray[activeLightCount] != zone)
            {
                _clearZoneArray[activeLightCount] = zone;
                hasChanges = true;
            }

            activeLightCount++;
        }

        // Update shader properties if there are changes
        if (hasChanges || _clearZonesChanged)
        {
            _material.SetInt(ClearZoneCountId, activeLightCount);

            // Update compute buffer data
            if (activeLightCount > 0)
            {
                _clearZonesBuffer.SetData(_clearZoneArray);
                _material.SetBuffer("_ClearZones", _clearZonesBuffer);
            }

            _material.SetVectorArray(ClearZonesId, _clearZoneArray);
            _clearZonesChanged = false;

#if UNITY_EDITOR
            Debug.Log($"FoliageController: Updated clear zones: {activeLightCount} active lights");

            // Detailed zone debug information
            for (int i = 0; i < activeLightCount; i++)
            {
                Debug.Log($"Zone {i}: Position({_clearZoneArray[i].x:F1}, {_clearZoneArray[i].y:F1}, {_clearZoneArray[i].z:F1}) Range: {_clearZoneArray[i].w:F1}");
            }
#endif
        }
    }

    /// <summary>
    /// Called when a light source changes state
    /// </summary>
    private void OnLightChanged(LightSourceComponent light)
    {
        _clearZonesChanged = true;
        Debug.Log($"FoliageController: Light changed: {light.name}, IsOn: {light.IsLightOn}");
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Unsubscribe from light service events to prevent memory leaks
    /// </summary>
    private void UnsubscribeFromLightEvents()
    {
        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight -= OnLightChanged;
            _lightService.OnSwitchOffLight -= OnLightChanged;
        }
    }

    /// <summary>
    /// Dispose all loaded chunks and clear collections
    /// </summary>
    private void DisposeAllChunks()
    {
        foreach (var chunk in _loadedChunks.Values)
        {
            chunk?.Dispose();
        }
        _loadedChunks.Clear();
        _loadingChunks.Clear();
        _chunkLoadQueue.Clear();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Number of currently loaded chunks
    /// </summary>
    public int LoadedChunkCount => _loadedChunks.Count;

    /// <summary>
    /// Number of chunks currently being loaded
    /// </summary>
    public int LoadingChunkCount => _loadingChunks.Count;

    /// <summary>
    /// Number of chunks queued for loading
    /// </summary>
    public int QueuedChunkCount => _chunkLoadQueue.Count;

    /// <summary>
    /// Current player chunk coordinates
    /// </summary>
    public Vector2Int CurrentPlayerChunk => _currentPlayerChunk;

    #endregion

    #region Debug Visualization

    /// <summary>
    /// Draw debug gizmos for chunk loading areas, render distances, and light zones
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!_player) return;

        // Draw chunk loading area
        Gizmos.color = Color.green;
        Vector3 center = _player.position;
        Vector3 size = new Vector3(
            _chunkSize.x * (_chunkLoadRadius * 2 + 1),
            0.1f,
            _chunkSize.y * (_chunkLoadRadius * 2 + 1)
        );
        Gizmos.DrawWireCube(center, size);

        // Draw loaded chunk bounds
        Gizmos.color = Color.blue;
        foreach (FoliageChunkInstance chunk in _loadedChunks.Values)
        {
            Gizmos.DrawWireSphere(chunk.ChunkCenter, chunk.ChunkRadius);
        }

        // Draw render distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_player.position, _renderDistance);

        // Draw cull distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_player.position, _cullDistance);

        // Draw light clear zones
        if (_clearZoneArray != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _clearZoneArray.Length; i++)
            {
                Vector4 zone = _clearZoneArray[i];
                if (zone.w > 0) // If zone has radius > 0
                {
                    Vector3 pos = new Vector3(zone.x, zone.y, zone.z);
                    Gizmos.DrawWireSphere(pos, zone.w);
                }
            }
        }
    }

    #endregion
}