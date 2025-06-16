using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Game.Services.LightSources;
using System.Linq;
using System.Collections;

public class FoliageController : MonoBehaviour
{
    #region Inspector Settings

    [Header("Rendering Settings")]
    [SerializeField] private Material _material;

    [SerializeField] private float _renderDistance = 100f;

    [SerializeField] private float _cullDistance = 150f;

    [SerializeField] private float[] _lodDistances = { 50f, 100f, 200f, 500f };

    [SerializeField] private bool _enableLOD = true;

    [SerializeField] private bool _enableFrustumCulling = true;

    [Header("Animation Settings")]
    [SerializeField] private bool _enableAnimation = false;

    [SerializeField] private float _animationDuration = 1.0f;

    [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private bool _enableLightBasedAnimation = true;

    [Header("Chunk Loading")]
    [SerializeField] private int _chunkLoadRadius = 1;

    [SerializeField] private string _chunkFolder = "FoliageChunks";

    [SerializeField] private int _maxConcurrentLoads = 2;

    [SerializeField] private float _loadFrameBudgetMs = 5f;

    [Header("Chunk Configuration")]

    [SerializeField] private Vector2 _chunkSize = new Vector2(100f, 100f);

    [Header("Performance")]
    [SerializeField] private int _maxChunksDrawnPerFrame = 50;

    [SerializeField] private bool _enableOcclusion = false;

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = false;

    [SerializeField] private bool _showMemoryUsage = false;

    #endregion

    #region Core References

    private LightSourcesService _lightService = null;

    private Transform _player;

    private Camera _playerCamera;

    #endregion

    #region Chunk Management System


    private Dictionary<string, FoliageChunkInstance> _loadedChunks = new();


    private Dictionary<string, ChunkLoadOperation> _loadingChunks = new();

    private Vector2Int _currentPlayerChunk = Vector2Int.zero;

    private Vector2Int _lastPlayerChunk = Vector2Int.one * int.MaxValue;


    private struct ChunkLoadOperation
    {
        public string chunkKey;        
        public Vector2Int chunkPos;     
        public float priority;           
        public System.DateTime startTime; 
    }

    #endregion

    #region Spatial Grid System for Light Animations


    private Dictionary<Vector2Int, List<InstanceRef>> _spatialGrid = new();

    private const float GRID_CELL_SIZE = 10f;


    private struct InstanceRef
    {
        public FoliageChunkInstance chunk;  
        public int localIndex;             
        public Vector3 worldPosition;       

        public InstanceRef(FoliageChunkInstance chunk, int localIndex, Vector3 worldPosition)
        {
            this.chunk = chunk;
            this.localIndex = localIndex;
            this.worldPosition = worldPosition;
        }
    }

    #endregion

    #region Animation System

    private Dictionary<int, float> _instanceAnimationStartTimes = new();


    private HashSet<int> _animatingInstances = new();

    #endregion

    #region Async Loading System

    private Queue<ChunkLoadOperation> _chunkLoadQueue = new();

    private bool _isLoadingChunks = false;

    private int _currentConcurrentLoads = 0;

    #endregion

    #region Performance Optimization Caches


    private readonly HashSet<string> _visibleChunkKeys = new HashSet<string>();

    private readonly List<string> _chunksToUnload = new List<string>();

    private float _lastChunkUpdateTime = 0f;

    private const float CHUNK_UPDATE_INTERVAL = 0.1f;

    #endregion

    #region Shader Communication

    private static readonly int ClearZoneCountId = Shader.PropertyToID("_ClearZoneCount");
    private static readonly int FlowTimeId = Shader.PropertyToID("_FlowTime");
    private static readonly int AnimationDurationId = Shader.PropertyToID("_AnimationDuration");
    private ComputeBuffer _clearZonesBuffer;
    private static readonly int ClearZonesPropertyId = Shader.PropertyToID("_ClearZones");

    #endregion

    #region Debug and Profiling

    private long _totalMemoryUsage = 0;

    private float _lastMemoryUpdateTime = 0f;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeReferences();

        if (_enableAnimation && _enableLightBasedAnimation)
        {
            InitializeLightSystem();
        }

        if (_player)
        {
            UpdatePlayerChunk(true);
        }
    }

    private void Update()
    {
        if (!_player) return;

        UpdateClearZonesBuffer();

        _material.SetFloat(FlowTimeId, Time.time);
        if (_enableAnimation)
        {
            _material.SetFloat(AnimationDurationId, _animationDuration);
        }

        if (_enableAnimation && _enableLightBasedAnimation)
        {
            UpdateAnimations();
        }

        if (Time.time - _lastChunkUpdateTime >= CHUNK_UPDATE_INTERVAL)
        {
            UpdatePlayerChunk();
            _lastChunkUpdateTime = Time.time;
        }

        DrawVisibleChunks();

        if (_showMemoryUsage && Time.time - _lastMemoryUpdateTime > 1f)
        {
            UpdateMemoryUsage();
            _lastMemoryUpdateTime = Time.time;
        }

        if (!_isLoadingChunks && _chunkLoadQueue.Count > 0 && _currentConcurrentLoads < _maxConcurrentLoads)
        {
            StartCoroutine(LoadChunkAsync());
        }
    }



    /// <summary>
    /// Updates the clear zones buffer with current light positions and ranges.
    /// This buffer is required by the shader for grass scaling calculations.
    /// </summary>
    private void UpdateClearZonesBuffer()
    {
        if (!_material) return;

        // Handle case when animation is disabled
        if (!_enableAnimation || !_enableLightBasedAnimation || _lightService == null)
        {
            _material.SetInt(ClearZoneCountId, 0);
            if (_clearZonesBuffer != null)
            {
                _clearZonesBuffer.Release();
                _clearZonesBuffer = null;
            }
            return;
        }

        // Collect active light zones
        var activeLights = new List<Vector4>();

        foreach (var light in _lightService.LightSources)
        {
            if (light != null && light.IsLightOn)
            {
                Vector3 pos = light.transform.position;
                float range = light.Settings.BrightnessRange;
                activeLights.Add(new Vector4(pos.x, pos.y, pos.z, range));
            }
        }

        // Create or resize buffer if needed
        int bufferSize = Mathf.Max(1, _lightService.TotalLightSources); // At least 1 to avoid zero-size buffer

        if (_clearZonesBuffer == null || _clearZonesBuffer.count != bufferSize)
        {
            _clearZonesBuffer?.Release();
            _clearZonesBuffer = new ComputeBuffer(bufferSize, sizeof(float) * 4);
        }

        // Create array with proper size
        Vector4[] clearZonesData = new Vector4[bufferSize];

        // Fill with active light data
        for (int i = 0; i < bufferSize; i++)
        {
            if (i < activeLights.Count)
            {
                clearZonesData[i] = activeLights[i];
            }
            else
            {
                clearZonesData[i] = Vector4.zero; // Fill remaining with zeros
            }
        }

        // Update buffer and shader properties
        _clearZonesBuffer.SetData(clearZonesData);
        _material.SetBuffer(ClearZonesPropertyId, _clearZonesBuffer);
        _material.SetInt(ClearZoneCountId, activeLights.Count);

        if (_showDebugInfo)
        {
            Debug.Log($"Updated clear zones buffer: {activeLights.Count} active lights out of {bufferSize} total");
        }
    }

    private void OnDisable()
    {
        if (_enableAnimation && _enableLightBasedAnimation)
        {
            UnsubscribeFromLightEvents();
        }

        if (_clearZonesBuffer != null)
        {
            _clearZonesBuffer.Release();
            _clearZonesBuffer = null;
        }

        DisposeAllChunks();
    }

    private void OnDestroy()
    {
        if (_enableAnimation && _enableLightBasedAnimation)
        {
            UnsubscribeFromLightEvents();
        }

        if (_clearZonesBuffer != null)
        {
            _clearZonesBuffer.Release();
            _clearZonesBuffer = null;
        }

        DisposeAllChunks();
    }

    #endregion

    #region Initialization


    private void InitializeReferences()
    {
        _player = FindFirstObjectByType<PlayerController>()?.transform;
        if (!_player)
        {
            Debug.LogError("FoliageController: No PlayerController found. Component disabled.");
            enabled = false;
            return;
        }

        _playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        if (!_playerCamera)
        {
            Debug.LogWarning("FoliageController: No main camera found, frustum culling disabled.");
            _enableFrustumCulling = false;
        }

        _lightService = LightSourcesService.Instance;
    }

    private void InitializeLightSystem()
    {
        if (_lightService == null)
        {
            Debug.LogWarning("FoliageController: LightSourcesService.Instance is null. Light-based animation disabled.");
            _enableLightBasedAnimation = false;
            return;
        }

        if (_lightService.TotalLightSources <= 0)
        {
            Debug.LogWarning("FoliageController: No light sources found. Light-based animation disabled.");
            _enableLightBasedAnimation = false;
            return;
        }

        _lightService.OnSwitchOnLight += OnLightChangedOptimized;
        _lightService.OnSwitchOffLight += OnLightChangedOptimized;

        Debug.Log($"FoliageController: Light system initialized with {_lightService.TotalLightSources} light sources");
    }

    #endregion

    #region Spatial Grid Management

    private Vector2Int WorldPositionToGridCell(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / GRID_CELL_SIZE),
            Mathf.FloorToInt(worldPos.z / GRID_CELL_SIZE)
        );
    }


    private void RegisterChunkInSpatialGrid(FoliageChunkInstance chunk)
    {
        if (!_enableAnimation || !chunk.AnimationEnabled) return;

        for (int i = 0; i < chunk.InstanceCount; i++)
        {
            Vector3 worldPos = chunk.GetInstanceWorldPosition(i);
            Vector2Int gridCell = WorldPositionToGridCell(worldPos);

            if (!_spatialGrid.ContainsKey(gridCell))
            {
                _spatialGrid[gridCell] = new List<InstanceRef>();
            }

            _spatialGrid[gridCell].Add(new InstanceRef(chunk, i, worldPos));
        }

        if (_showDebugInfo)
        {
            //Debug.Log($"Registered {chunk.InstanceCount} instances in spatial grid for chunk");
        }
    }

    private void UnregisterChunkFromSpatialGrid(FoliageChunkInstance chunk)
    {
        if (!_enableAnimation || !chunk.AnimationEnabled) return;

        var cellsToClean = new List<Vector2Int>();

        foreach (var kvp in _spatialGrid)
        {
            kvp.Value.RemoveAll(instanceRef => instanceRef.chunk == chunk);

            if (kvp.Value.Count == 0)
            {
                cellsToClean.Add(kvp.Key);
            }
        }

        foreach (var cell in cellsToClean)
        {
            _spatialGrid.Remove(cell);
        }
    }

    #endregion

    #region Optimized Light-Based Animation System


    private void OnLightChangedOptimized(LightSourceComponent light)
    {
        if (!_enableAnimation || !_enableLightBasedAnimation) return;

        Vector3 lightPos = light.transform.position;
        float lightRange = light.Settings.BrightnessRange;
        bool isLightOn = light.IsLightOn;

        int cellRadius = Mathf.CeilToInt(lightRange / GRID_CELL_SIZE);
        Vector2Int centerCell = WorldPositionToGridCell(lightPos);

        float currentTime = Time.time;
        int affectedInstances = 0;
        var affectedChunks = new HashSet<FoliageChunkInstance>();

        for (int dx = -cellRadius; dx <= cellRadius; dx++)
        {
            for (int dz = -cellRadius; dz <= cellRadius; dz++)
            {
                Vector2Int cellKey = new Vector2Int(centerCell.x + dx, centerCell.y + dz);

                if (!_spatialGrid.ContainsKey(cellKey)) continue;

                var instancesInCell = _spatialGrid[cellKey];

                foreach (var instanceRef in instancesInCell)
                {
                    float distance = Vector3.Distance(instanceRef.worldPosition, lightPos);
                    if (distance > lightRange * 1.1f) continue;

                    int globalId = instanceRef.chunk.GetGlobalInstanceId(instanceRef.localIndex);
                    if (globalId >= 0 && !_animatingInstances.Contains(globalId))
                    {
                        _instanceAnimationStartTimes[globalId] = currentTime;
                        _animatingInstances.Add(globalId);
                        affectedInstances++;

                        affectedChunks.Add(instanceRef.chunk);
                    }
                }
            }
        }

        foreach (var chunk in affectedChunks)
        {
            chunk.UpdateAnimationStartTimes(_instanceAnimationStartTimes);
        }

        if (_showDebugInfo)
        {
            //Debug.Log($"Light {light.name} {(isLightOn ? "ON" : "OFF")}: affected {affectedInstances} instances in {cellRadius * 2 + 1}x{cellRadius * 2 + 1} grid cells");
        }
    }

    private void UpdateAnimations()
    {
        if (!_enableAnimation || _animatingInstances.Count == 0) return;

        float currentTime = Time.time;
        var completedAnimations = new List<int>();

        foreach (int instanceId in _animatingInstances)
        {
            if (_instanceAnimationStartTimes.TryGetValue(instanceId, out float startTime))
            {
                float elapsed = currentTime - startTime;
                if (elapsed >= _animationDuration)
                {
                    completedAnimations.Add(instanceId);
                }
            }
        }

        foreach (int instanceId in completedAnimations)
        {
            _animatingInstances.Remove(instanceId);
            _instanceAnimationStartTimes.Remove(instanceId);
        }
    }

    #endregion

    #region Chunk Management

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

    private void LoadChunksAround(Vector2Int center)
    {
        _visibleChunkKeys.Clear();
        var newLoadOperations = new List<ChunkLoadOperation>();

        for (int dx = -_chunkLoadRadius; dx <= _chunkLoadRadius; dx++)
        {
            for (int dz = -_chunkLoadRadius; dz <= _chunkLoadRadius; dz++)
            {
                Vector2Int chunkPos = new Vector2Int(center.x + dx, center.y + dz);
                string chunkKey = $"{chunkPos.x}_{chunkPos.y}";
                _visibleChunkKeys.Add(chunkKey);

                float distance = new Vector2(dx, dz).magnitude;

                if (!_loadedChunks.ContainsKey(chunkKey) && !_loadingChunks.ContainsKey(chunkKey))
                {
                    newLoadOperations.Add(new ChunkLoadOperation
                    {
                        chunkKey = chunkKey,
                        chunkPos = chunkPos,
                        priority = distance, 
                        startTime = System.DateTime.Now
                    });
                }
            }
        }

        newLoadOperations.Sort((a, b) => a.priority.CompareTo(b.priority));
        foreach (var op in newLoadOperations)
        {
            _chunkLoadQueue.Enqueue(op);
            _loadingChunks[op.chunkKey] = op;
        }

        UnloadDistantChunks();
    }

    private void UnloadDistantChunks()
    {
        _chunksToUnload.Clear();

        foreach (var chunkId in _loadedChunks.Keys)
        {
            if (!_visibleChunkKeys.Contains(chunkId))
            {
                _chunksToUnload.Add(chunkId);
            }
        }

        foreach (var chunkId in _chunksToUnload)
        {
            var chunk = _loadedChunks[chunkId];
            UnregisterChunkFromSpatialGrid(chunk); 
            chunk.Dispose();                       
            _loadedChunks.Remove(chunkId);
        }

        var loadingToCancel = _loadingChunks.Where(kvp => !_visibleChunkKeys.Contains(kvp.Key)).ToList();
        foreach (var kvp in loadingToCancel)
        {
            _loadingChunks.Remove(kvp.Key);
        }
    }

    #endregion

    #region Async Chunk Loading

    private IEnumerator LoadChunkAsync()
    {
        _isLoadingChunks = true;
        _currentConcurrentLoads++;

        float frameStartTime = Time.realtimeSinceStartup * 1000f;
        int chunksLoadedThisFrame = 0;
        const int MAX_CHUNKS_PER_FRAME = 1; 

        while (_chunkLoadQueue.Count > 0)
        {
            float currentTime = Time.realtimeSinceStartup * 1000f;

            if (chunksLoadedThisFrame >= MAX_CHUNKS_PER_FRAME ||
                currentTime - frameStartTime > _loadFrameBudgetMs)
            {
                yield return null; 
                frameStartTime = Time.realtimeSinceStartup * 1000f;
                chunksLoadedThisFrame = 0;
            }

            var loadOp = _chunkLoadQueue.Dequeue();

            if (!_visibleChunkKeys.Contains(loadOp.chunkKey))
            {
                _loadingChunks.Remove(loadOp.chunkKey);
                continue;
            }

            if (!_loadedChunks.ContainsKey(loadOp.chunkKey))
            {
                TryLoadChunk(loadOp.chunkKey);
                chunksLoadedThisFrame++;

                yield return null;
            }

            _loadingChunks.Remove(loadOp.chunkKey);
        }

        _currentConcurrentLoads--;
        _isLoadingChunks = false;
    }

    private void TryLoadChunk(string chunkKey)
    {
        string path = Path.Combine(_chunkFolder, chunkKey);

        var request = Resources.LoadAsync<TextAsset>(path);

        if (request == null)
        {
            if (_showDebugInfo)
            {
                Debug.LogWarning($"FoliageController: Chunk file not found: {path}");
            }
            return;
        }

        StartCoroutine(WaitForChunkLoad(request, chunkKey));
    }

    private IEnumerator WaitForChunkLoad(ResourceRequest request, string chunkKey)
    {
        yield return request;

        TextAsset json = request.asset as TextAsset;
        if (!json)
        {
            if (_showDebugInfo)
            {
                Debug.LogWarning($"FoliageController: Failed to load chunk: {chunkKey}");
            }
            yield break;
        }

        try
        {
            ChunkData data = JsonUtility.FromJson<ChunkData>(json.text);
            if (data?.MeshDatas == null)
            {
                Debug.LogWarning($"FoliageController: Invalid chunk data: {chunkKey}");
                yield break;
            }

            var chunkInstance = FoliageChunkFactory.Create(data, _material, _enableAnimation);
            _loadedChunks.Add(chunkKey, chunkInstance);

            RegisterChunkInSpatialGrid(chunkInstance);

            if (_showDebugInfo)
            {
                //Debug.Log($"FoliageController: Loaded chunk {chunkKey} with {chunkInstance.TotalInstances} instances (Animation: {chunkInstance.AnimationEnabled})");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"FoliageController: Error loading chunk {chunkKey}: {e.Message}");
        }
    }

    #endregion

    #region Rendering System

    private void DrawVisibleChunks()
    {
        if (_loadedChunks.Count <= 0) return;

        int chunksDrawn = 0;
        Vector3 playerPos = _player.position;
        Camera camera = _enableFrustumCulling ? _playerCamera : null;

        foreach (var kvp in _loadedChunks)
        {
            if (chunksDrawn >= _maxChunksDrawnPerFrame) break;

            var chunk = kvp.Value;

            if (Vector3.Distance(playerPos, chunk.ChunkCenter) > _cullDistance + chunk.ChunkRadius)
                continue;

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

    #region Performance Monitoring and Debug

    private void UpdateMemoryUsage()
    {
        _totalMemoryUsage = 0;
        foreach (var chunk in _loadedChunks.Values)
        {
            _totalMemoryUsage += FoliageRenderingUtils.CalculateMemoryUsage(chunk);
        }

        if (_showMemoryUsage)
        {
            Debug.Log($"FoliageController Memory Usage: {_totalMemoryUsage / (1024 * 1024):F2} MB across {_loadedChunks.Count} chunks");
        }
    }

    #endregion

    #region Cleanup and Resource Management


    private void UnsubscribeFromLightEvents()
    {
        if (_lightService != null)
        {
            _lightService.OnSwitchOnLight -= OnLightChangedOptimized;
            _lightService.OnSwitchOffLight -= OnLightChangedOptimized;
        }
    }

    private void DisposeAllChunks()
    {
        foreach (var chunk in _loadedChunks.Values)
        {
            UnregisterChunkFromSpatialGrid(chunk); 
            chunk?.Dispose();                      
        }

        _loadedChunks.Clear();
        _loadingChunks.Clear();
        _chunkLoadQueue.Clear();
        _spatialGrid.Clear(); 

        if (_enableAnimation)
        {
            _instanceAnimationStartTimes.Clear();
            _animatingInstances.Clear();
        }

        if (_clearZonesBuffer != null)
        {
            _clearZonesBuffer.Release();
            _clearZonesBuffer = null;
        }
    }

    #endregion

    #region Public Interface

    public int LoadedChunkCount => _loadedChunks.Count;

    public int LoadingChunkCount => _loadingChunks.Count;

    public int QueuedChunkCount => _chunkLoadQueue.Count;

    public Vector2Int CurrentPlayerChunk => _currentPlayerChunk;

    public int ActiveAnimationCount => _enableAnimation ? _animatingInstances.Count : 0;

    public long TotalMemoryUsage => _totalMemoryUsage;

    public bool AnimationEnabled => _enableAnimation;

    public bool LightBasedAnimationEnabled => _enableAnimation && _enableLightBasedAnimation;

    public int SpatialGridCellCount => _spatialGrid.Count;

    public void SetAnimationEnabled(bool enabled)
    {
        if (_enableAnimation != enabled)
        {
            _enableAnimation = enabled;

            if (!enabled)
            {
                _instanceAnimationStartTimes.Clear();
                _animatingInstances.Clear();
            }

            Debug.Log($"FoliageController: Animation {(enabled ? "enabled" : "disabled")}");
        }
    }

    public void SetChunkLoadRadius(int radius)
    {
        _chunkLoadRadius = Mathf.Max(0, radius);
        if (_player)
        {
            UpdatePlayerChunk(true);
        }
        Debug.Log($"FoliageController: Chunk load radius set to {_chunkLoadRadius}");
    }

    [ContextMenu("Trigger All Animations")]
    public void TriggerAllAnimations()
    {
        if (!_enableAnimation) return;

        float currentTime = Time.time;
        int animationCount = 0;

        foreach (var chunk in _loadedChunks.Values)
        {
            if (!chunk.AnimationEnabled) continue;

            for (int i = 0; i < chunk.InstanceCount; i++)
            {
                int globalId = chunk.GetGlobalInstanceId(i);
                if (globalId >= 0)
                {
                    _instanceAnimationStartTimes[globalId] = currentTime;
                    _animatingInstances.Add(globalId);
                    animationCount++;
                }
            }

            chunk.UpdateAnimationStartTimes(_instanceAnimationStartTimes);
        }

        Debug.Log($"Triggered animations for all {animationCount} visible instances");
    }

    [ContextMenu("Reload All Chunks")]
    public void ReloadAllChunks()
    {
        Debug.Log("FoliageController: Reloading all chunks...");

        Vector2Int currentChunk = _currentPlayerChunk;

        DisposeAllChunks();

        _currentPlayerChunk = Vector2Int.one * int.MaxValue; 
        UpdatePlayerChunk(true);

        Debug.Log($"FoliageController: Reloaded {_loadedChunks.Count} chunks");
    }

    [ContextMenu("Rebuild Spatial Grid")]
    public void RebuildSpatialGrid()
    {
        if (!_enableAnimation) return;

        Debug.Log("FoliageController: Rebuilding spatial grid...");

        _spatialGrid.Clear();

        foreach (var chunk in _loadedChunks.Values)
        {
            RegisterChunkInSpatialGrid(chunk);
        }

        Debug.Log($"FoliageController: Spatial grid rebuilt with {_spatialGrid.Count} cells");
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (!_player) return;

        Gizmos.color = Color.green;
        Vector3 center = _player.position;
        Vector3 size = new Vector3(
            _chunkSize.x * (_chunkLoadRadius * 2 + 1),
            0.1f,
            _chunkSize.y * (_chunkLoadRadius * 2 + 1)
        );
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.blue;
        foreach (var chunk in _loadedChunks.Values)
        {
            FoliageRenderingUtils.DrawChunkBounds(chunk.ChunkCenter, chunk.ChunkRadius, Color.blue);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_player.position, _renderDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_player.position, _cullDistance);

        if (_enableAnimation && _enableLightBasedAnimation && _lightService != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var lightSource in _lightService.LightSources)
            {
                if (lightSource != null && lightSource.IsLightOn)
                {
                    Vector3 pos = lightSource.transform.position;
                    float range = lightSource.Settings.BrightnessRange;
                    Gizmos.DrawWireSphere(pos, range);
                }
            }
        }

        if (_enableAnimation && _spatialGrid.Count > 0 && _showDebugInfo)
        {
            Gizmos.color = new Color(1, 0, 1, 0.1f);
            foreach (var cellPos in _spatialGrid.Keys)
            {
                Vector3 worldPos = new Vector3(
                    cellPos.x * GRID_CELL_SIZE + GRID_CELL_SIZE * 0.5f,
                    _player.position.y,
                    cellPos.y * GRID_CELL_SIZE + GRID_CELL_SIZE * 0.5f
                );
                Gizmos.DrawCube(worldPos, new Vector3(GRID_CELL_SIZE, 0.1f, GRID_CELL_SIZE));
            }
        }

        if (_enableAnimation && _animatingInstances.Count > 0)
        {
            Gizmos.color = Color.magenta;
            foreach (var chunk in _loadedChunks.Values)
            {
                if (!chunk.AnimationEnabled) continue;

                for (int i = 0; i < chunk.InstanceCount; i++)
                {
                    int globalId = chunk.GetGlobalInstanceId(i);
                    if (globalId >= 0 && _animatingInstances.Contains(globalId))
                    {
                        Vector3 instancePos = chunk.GetInstanceWorldPosition(i);
                        Gizmos.DrawWireCube(instancePos, Vector3.one * 0.5f);
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!_showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 300));
        GUILayout.Label("=== FOLIAGE CONTROLLER DEBUG ===");

        GUILayout.Space(5);
        GUILayout.Label("--- CHUNK SYSTEM ---");
        GUILayout.Label($"Loaded Chunks: {LoadedChunkCount}");
        GUILayout.Label($"Loading Chunks: {LoadingChunkCount}");
        GUILayout.Label($"Queued Chunks: {QueuedChunkCount}");
        GUILayout.Label($"Player Chunk: {CurrentPlayerChunk}");
        GUILayout.Label($"Chunk Load Radius: {_chunkLoadRadius}");

        if (_enableAnimation)
        {
            GUILayout.Space(5);
            GUILayout.Label("--- ANIMATION SYSTEM ---");
            GUILayout.Label($"Active Animations: {ActiveAnimationCount}");
            GUILayout.Label($"Animation Duration: {_animationDuration:F1}s");
            GUILayout.Label($"Spatial Grid Cells: {SpatialGridCellCount}");

            int totalInstancesInGrid = 0;
            foreach (var cellList in _spatialGrid.Values)
            {
                totalInstancesInGrid += cellList.Count;
            }
            GUILayout.Label($"Instances in Grid: {totalInstancesInGrid}");
        }

        if (_showMemoryUsage)
        {
            GUILayout.Space(5);
            GUILayout.Label("--- MEMORY USAGE ---");
            GUILayout.Label($"Total Memory: {TotalMemoryUsage / (1024 * 1024):F2} MB");

            if (LoadedChunkCount > 0)
            {
                float avgMemoryPerChunk = (TotalMemoryUsage / (float)LoadedChunkCount) / (1024 * 1024);
                GUILayout.Label($"Avg per Chunk: {avgMemoryPerChunk:F2} MB");
            }
        }

        if (_enableAnimation && _enableLightBasedAnimation && _lightService != null)
        {
            GUILayout.Space(5);
            GUILayout.Label("--- LIGHT SYSTEM ---");
            GUILayout.Label($"Total Lights: {_lightService.TotalLightSources}");

            int activeLights = 0;
            foreach (var light in _lightService.LightSources)
            {
                if (light != null && light.IsLightOn)
                    activeLights++;
            }
            GUILayout.Label($"Active Lights: {activeLights}");
        }

        GUILayout.Space(5);
        GUILayout.Label("--- PERFORMANCE ---");
        GUILayout.Label($"Target FPS: {1000f / Time.deltaTime:F0}");
        GUILayout.Label($"Frame Time: {Time.deltaTime * 1000f:F1}ms");

        int totalInstances = 0;
        foreach (var chunk in _loadedChunks.Values)
        {
            totalInstances += chunk.TotalInstances;
        }
        GUILayout.Label($"Total Instances: {totalInstances}");

        GUILayout.Space(10);
        GUILayout.Label("--- DEBUG ACTIONS ---");

        if (GUILayout.Button("Trigger All Animations"))
        {
            TriggerAllAnimations();
        }

        if (GUILayout.Button("Reload All Chunks"))
        {
            ReloadAllChunks();
        }

        if (_enableAnimation && GUILayout.Button("Rebuild Spatial Grid"))
        {
            RebuildSpatialGrid();
        }

        if (_enableAnimation)
        {
            bool oldShowGrid = _showDebugInfo;
            bool newShowGrid = GUILayout.Toggle(oldShowGrid, "Show Spatial Grid");
            if (newShowGrid != oldShowGrid)
            {
                _showDebugInfo = newShowGrid;
            }
        }

        GUILayout.EndArea();
    }

    #endregion
}