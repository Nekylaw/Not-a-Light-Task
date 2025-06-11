using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unified foliage chunk instance that supports both standard and animated rendering.
/// Animation features are enabled based on constructor parameters.
/// </summary>
public class FoliageChunkInstance
{
    private struct MeshGroup
    {
        public Mesh mesh;
        public int startOffset;
        public int count;
        public Vector3 center;
        public float boundingRadius;
        public ComputeBuffer argsBuffer;
        public Bounds actualBounds;
    }

    // Chunk data
    private Vector3 _chunkCenter;
    private float _chunkRadius;
    private int _totalInstances;

    // Core rendering data
    private List<MeshGroup> _meshGroups = new();
    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _scaleBuffer;
    private Material _material;
    private MaterialPropertyBlock _propertyBlock;

    // Animation system (optional)
    private readonly bool _animationEnabled;
    private ComputeBuffer _animationStartTimesBuffer;
    private float[] _animationStartTimes;
    private int _globalInstanceIdOffset;
    private static int _nextGlobalInstanceId = 0;
    private List<Vector3> _instanceWorldPositions; // Cache for animation system

    // Shader property IDs
    private static readonly int MatricesPropertyId = Shader.PropertyToID("_Matrices");
    private static readonly int BaseScalesPropertyId = Shader.PropertyToID("_BaseScales");
    private static readonly int MatrixOffsetPropertyId = Shader.PropertyToID("_MatrixOffset");
    private static readonly int FlowTimePropertyId = Shader.PropertyToID("_FlowTime");
    private static readonly int AnimationStartTimesPropertyId = Shader.PropertyToID("_AnimationStartTimes");

    #region Constructors

    /// <summary>
    /// Create a standard foliage chunk instance without animation support
    /// </summary>
    public FoliageChunkInstance(ChunkData chunk, Material material)
        : this(chunk, material, false) { }

    /// <summary>
    /// Create a foliage chunk instance with optional animation support
    /// </summary>
    public FoliageChunkInstance(ChunkData chunk, Material material, bool enableAnimation)
    {
        _material = material;
        _propertyBlock = new MaterialPropertyBlock();
        _animationEnabled = enableAnimation;

        InitializeChunkData(chunk);

        if (_animationEnabled)
        {
            InitializeAnimationSystem();
        }
    }

    #endregion

    #region Initialization

    private void InitializeChunkData(ChunkData chunk)
    {
        List<Matrix4x4> matrices = new();
        List<Vector4> scales = new();
        List<Vector3> worldPositions = new();
        Vector3 chunkCenter = Vector3.zero;
        int totalInstances = 0;

        foreach (var meshData in chunk.MeshDatas)
        {
            Mesh mesh = Resources.FindObjectsOfTypeAll<Mesh>().FirstOrDefault(m => m.name == meshData.MeshName);
            if (!mesh)
            {
                Debug.LogWarning($"Mesh not found: {meshData.MeshName}");
                continue;
            }

            int startOffset = matrices.Count;
            Vector3 groupCenter = Vector3.zero;
            float maxDistanceFromCenter = 0f;

            // Calculate actual bounds for this mesh group
            Bounds meshBounds = mesh.bounds;
            Bounds groupBounds = new Bounds();
            bool boundsInitialized = false;

            foreach (var matrice in meshData.Matrices)
            {
                Matrix4x4 m = matrice.ToMatrix();
                Vector3 position = (Vector3)m.GetColumn(3);
                Vector3 scale = m.lossyScale;

                scales.Add(new Vector4(scale.x, scale.y, scale.z, 1f));
                matrices.Add(m);
                worldPositions.Add(position); // Store for animation system
                groupCenter += position;
                totalInstances++;

                // Calculate actual bounds for this instance
                Bounds instanceBounds = new Bounds(position, meshBounds.size);
                instanceBounds.size = Vector3.Scale(instanceBounds.size, scale);

                if (!boundsInitialized)
                {
                    groupBounds = instanceBounds;
                    boundsInitialized = true;
                }
                else
                {
                    groupBounds.Encapsulate(instanceBounds);
                }
            }

            if (meshData.Matrices.Count > 0)
            {
                groupCenter /= meshData.Matrices.Count;

                // Calculate bounding radius more accurately
                foreach (var matrice in meshData.Matrices)
                {
                    Vector3 pos = (Vector3)matrice.ToMatrix().GetColumn(3);
                    float distance = Vector3.Distance(pos, groupCenter);
                    if (distance > maxDistanceFromCenter)
                        maxDistanceFromCenter = distance;
                }

                var meshGroup = new MeshGroup
                {
                    mesh = mesh,
                    startOffset = startOffset,
                    count = meshData.Matrices.Count,
                    center = groupCenter,
                    boundingRadius = maxDistanceFromCenter + meshBounds.size.magnitude * 0.5f,
                    actualBounds = groupBounds
                };

                _meshGroups.Add(meshGroup);
                chunkCenter += groupCenter;
            }
        }

        _totalInstances = totalInstances;
        _chunkCenter = _meshGroups.Count > 0 ? chunkCenter / _meshGroups.Count : Vector3.zero;
        _instanceWorldPositions = worldPositions;

        // Calculate chunk radius
        _chunkRadius = 0f;
        foreach (var group in _meshGroups)
        {
            float distance = Vector3.Distance(group.center, _chunkCenter) + group.boundingRadius;
            if (distance > _chunkRadius)
                _chunkRadius = distance;
        }

        // Create buffers
        if (matrices.Count > 0)
        {
            _matrixBuffer = new ComputeBuffer(matrices.Count, sizeof(float) * 16);
            _matrixBuffer.SetData(matrices);

            _scaleBuffer = new ComputeBuffer(scales.Count, sizeof(float) * 4);
            _scaleBuffer.SetData(scales);
        }

        CreateArgsBuffers();
    }

    private void InitializeAnimationSystem()
    {
        if (!_animationEnabled || _totalInstances == 0) return;

        // Assign global instance IDs
        _globalInstanceIdOffset = _nextGlobalInstanceId;
        _nextGlobalInstanceId += _totalInstances;

        // Initialize animation timing data
        _animationStartTimes = new float[_totalInstances];
        _animationStartTimesBuffer = new ComputeBuffer(
            Mathf.Max(1, _totalInstances),
            sizeof(float)
        );

        // Initialize all animation times to 0 (no animation)
        for (int i = 0; i < _totalInstances; i++)
        {
            _animationStartTimes[i] = 0f;
        }

        _animationStartTimesBuffer.SetData(_animationStartTimes);

        //Debug.Log($"Animation system initialized for chunk with {_totalInstances} instances, global ID offset: {_globalInstanceIdOffset}");
    }

    private void CreateArgsBuffers()
    {
        for (int i = 0; i < _meshGroups.Count; i++)
        {
            var group = _meshGroups[i];

            uint[] args = new uint[5] {
                group.mesh.GetIndexCount(0),
                (uint)group.count,
                group.mesh.GetIndexStart(0),
                group.mesh.GetBaseVertex(0),
                0
            };

            ComputeBuffer argsBuf = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuf.SetData(args);

            var updatedGroup = group;
            updatedGroup.argsBuffer = argsBuf;
            _meshGroups[i] = updatedGroup;
        }
    }

    #endregion

    #region Rendering

    public void Draw(Vector3 playerPos, float renderDist, float cullDist, Camera camera = null)
    {
        // Early chunk-level culling
        float distanceToChunk = Vector3.Distance(playerPos, _chunkCenter);
        if (distanceToChunk - _chunkRadius > cullDist)
            return;

        // Chunk-level frustum culling
        if (camera != null)
        {
            var chunkBounds = new Bounds(_chunkCenter, Vector3.one * (_chunkRadius * 2f));
            if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), chunkBounds))
                return;
        }

        // Set buffers for this chunk using MaterialPropertyBlock
        SetupMaterialProperties();

        int drawnGroups = 0;

        for (int i = 0; i < _meshGroups.Count; i++)
        {
            var group = _meshGroups[i];

            // Group-level distance culling
            float distanceToGroup = Vector3.Distance(playerPos, group.center);
            if (distanceToGroup - group.boundingRadius > cullDist)
                continue;

            // Group-level frustum culling
            if (camera != null)
            {
                if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), group.actualBounds))
                    continue;
            }

            // Set matrix offset for this group
            _propertyBlock.SetInt(MatrixOffsetPropertyId, group.startOffset);

            // Use actual bounds for rendering
            Graphics.DrawMeshInstancedIndirect(
                group.mesh,
                0,
                _material,
                group.actualBounds,
                group.argsBuffer,
                0,
                _propertyBlock,
                ShadowCastingMode.Off,
                false,
                0,
                camera,
                LightProbeUsage.Off
            );

            drawnGroups++;
        }
    }

    public void DrawWithLOD(Vector3 playerPos, float[] lodDistances, float cullDist, Camera camera = null)
    {
        float distanceToChunk = Vector3.Distance(playerPos, _chunkCenter);
        if (distanceToChunk - _chunkRadius > cullDist)
            return;

        int lodLevel = GetLODLevel(distanceToChunk, lodDistances);

        // @todo implement LOD Use simpler meshes

        Draw(playerPos, lodDistances[lodLevel], cullDist, camera);
    }

    private void SetupMaterialProperties()
    {
        _propertyBlock.SetBuffer(MatricesPropertyId, _matrixBuffer);
        _propertyBlock.SetBuffer(BaseScalesPropertyId, _scaleBuffer);
        _propertyBlock.SetFloat(FlowTimePropertyId, Time.time);

        // Set animation buffer if enabled
        if (_animationEnabled && _animationStartTimesBuffer != null)
        {
            _propertyBlock.SetBuffer(AnimationStartTimesPropertyId, _animationStartTimesBuffer);
        }
    }

    private int GetLODLevel(float distance, float[] lodDistances)
    {
        for (int i = 0; i < lodDistances.Length; i++)
        {
            if (distance <= lodDistances[i])
                return i;
        }
        return lodDistances.Length - 1;
    }

    #endregion

    #region Animation System 

    /// <summary>
    /// Update animation start times for instances in this chunk (only if animation is enabled)
    /// </summary>
    public void UpdateAnimationStartTimes(Dictionary<int, float> globalAnimationTimes)
    {
        if (!_animationEnabled || _animationStartTimes == null) return;

        bool hasChanges = false;

        for (int i = 0; i < _animationStartTimes.Length; i++)
        {
            int globalId = _globalInstanceIdOffset + i;

            if (globalAnimationTimes.TryGetValue(globalId, out float startTime))
            {
                if (_animationStartTimes[i] != startTime)
                {
                    _animationStartTimes[i] = startTime;
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            _animationStartTimesBuffer.SetData(_animationStartTimes);
        }
    }

    /// <summary>
    /// Get the global instance ID for a local instance index (only if animation is enabled)
    /// </summary>
    public int GetGlobalInstanceId(int localIndex)
    {
        if (!_animationEnabled)
        {
            Debug.LogWarning("Animation is not enabled for this chunk instance");
            return -1;
        }

        return _globalInstanceIdOffset + localIndex;
    }

    /// <summary>
    /// Get world position of an instance (only if animation is enabled)
    /// </summary>
    public Vector3 GetInstanceWorldPosition(int localIndex)
    {
        if (!_animationEnabled || _instanceWorldPositions == null || localIndex >= _instanceWorldPositions.Count)
        {
            return _chunkCenter; // Fallback to chunk center
        }

        return _instanceWorldPositions[localIndex];
    }

    #endregion

    #region Public Properties

    public int MeshGroupCount => _meshGroups.Count;
    public Vector3 ChunkCenter => _chunkCenter;
    public float ChunkRadius => _chunkRadius;
    public int TotalInstances => _totalInstances;
    public int InstanceCount => _animationEnabled ? (_animationStartTimes?.Length ?? 0) : _totalInstances;
    public bool AnimationEnabled => _animationEnabled;

    #endregion

    #region Cleanup

    public virtual void Dispose()
    {
        _matrixBuffer?.Release();
        _scaleBuffer?.Release();

        // Dispose animation buffers if they exist
        if (_animationEnabled)
        {
            _animationStartTimesBuffer?.Release();
        }

        foreach (var group in _meshGroups)
        {
            group.argsBuffer?.Release();
        }

        _meshGroups.Clear();
        _instanceWorldPositions?.Clear();

        //Debug.Log($"Disposed chunk with {_totalInstances} instances (Animation: {_animationEnabled})");
    }

    #endregion
}

#region Utility Classes

/// <summary>
/// Factory class for creating foliage chunk instances with appropriate configuration
/// </summary>
public static class FoliageChunkFactory
{
    /// <summary>
    /// Create a standard foliage chunk instance
    /// </summary>
    public static FoliageChunkInstance CreateStandard(ChunkData chunk, Material material)
    {
        return new FoliageChunkInstance(chunk, material, false);
    }

    /// <summary>
    /// Create an animated foliage chunk instance
    /// </summary>
    public static FoliageChunkInstance CreateAnimated(ChunkData chunk, Material material)
    {
        return new FoliageChunkInstance(chunk, material, true);
    }

    /// <summary>
    /// Create a foliage chunk instance with specified animation support
    /// </summary>
    public static FoliageChunkInstance Create(ChunkData chunk, Material material, bool enableAnimation)
    {
        return new FoliageChunkInstance(chunk, material, enableAnimation);
    }
}

/// <summary>
/// Utility class for foliage rendering operations
/// </summary>
public static class FoliageRenderingUtils
{
    public static readonly float[] DefaultLODDistances = { 50f, 100f, 200f, 500f };

    public static bool IsChunkVisible(Vector3 chunkCenter, float chunkRadius, Vector3 playerPos, float maxDistance, Camera camera)
    {
        float distance = Vector3.Distance(playerPos, chunkCenter);
        if (distance - chunkRadius > maxDistance)
            return false;

        if (camera != null)
        {
            var bounds = new Bounds(chunkCenter, Vector3.one * (chunkRadius * 2f));
            return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera), bounds);
        }

        return true;
    }

    public static void DrawChunkBounds(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(center, radius);
    }

    /// <summary>
    /// Calculate memory usage for a foliage chunk instance
    /// </summary>
    public static long CalculateMemoryUsage(FoliageChunkInstance instance)
    {
        long memoryUsage = 0;

        // Matrix buffer: 16 floats per matrix
        memoryUsage += instance.TotalInstances * 16 * sizeof(float);

        // Scale buffer: 4 floats per scale
        memoryUsage += instance.TotalInstances * 4 * sizeof(float);

        // Animation buffer (if enabled): 1 float per instance
        if (instance.AnimationEnabled)
        {
            memoryUsage += instance.TotalInstances * sizeof(float);
        }

        // Args buffers: 5 uints per mesh group
        memoryUsage += instance.MeshGroupCount * 5 * sizeof(uint);

        return memoryUsage;
    }
}

#endregion