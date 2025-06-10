using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

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

    private List<MeshGroup> _meshGroups = new();
    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _scaleBuffer;
    private Material _material;
    private MaterialPropertyBlock _propertyBlock;
    private readonly Vector3 _chunkCenter;
    private readonly float _chunkRadius;
    private readonly int _totalInstances;

    // Shader property IDs
    private static readonly int MatricesPropertyId = Shader.PropertyToID("_Matrices");
    private static readonly int BaseScalesPropertyId = Shader.PropertyToID("_BaseScales");
    private static readonly int MatrixOffsetPropertyId = Shader.PropertyToID("_MatrixOffset");
    private static readonly int FlowTimePropertyId = Shader.PropertyToID("_FlowTime");

    public FoliageChunkInstance(ChunkData chunk, Material material)
    {
        _material = material;
        _propertyBlock = new MaterialPropertyBlock();

        List<Matrix4x4> matrices = new();
        List<Vector4> scales = new();
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

        //Debug.Log($"Chunk created: {_meshGroups.Count} mesh groups, {totalInstances} total instances, center: {_chunkCenter}, radius: {_chunkRadius}");
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
        _propertyBlock.SetBuffer(MatricesPropertyId, _matrixBuffer);
        _propertyBlock.SetBuffer(BaseScalesPropertyId, _scaleBuffer);
        _propertyBlock.SetFloat(FlowTimePropertyId, Time.time);

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

        //if (drawnGroups > 0)
        //{
        //    Debug.Log($"Drew {drawnGroups} groups from chunk at {_chunkCenter}, distance: {distanceToChunk:F1}");
        //}
    }

    public void DrawWithLOD(Vector3 playerPos, float[] lodDistances, float cullDist, Camera camera = null)
    {
        float distanceToChunk = Vector3.Distance(playerPos, _chunkCenter);
        if (distanceToChunk - _chunkRadius > cullDist)
            return;

        int lodLevel = GetLODLevel(distanceToChunk, lodDistances);

        // @todo one of below
        // 1. Skip some mesh groups based on distance
        // 2. Reduce instance count
        // 3. Use simpler meshes
        // But flemme franchement

        Draw(playerPos, lodDistances[lodLevel], cullDist, camera);
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

    public void Dispose()
    {
        _matrixBuffer?.Release();
        _scaleBuffer?.Release();

        foreach (var group in _meshGroups)
        {
            group.argsBuffer?.Release();
        }

        _meshGroups.Clear();

        Debug.Log($"Disposed chunk with {_totalInstances} instances");
    }

    // Properties
    public int MeshGroupCount => _meshGroups.Count;
    public Vector3 ChunkCenter => _chunkCenter;
    public float ChunkRadius => _chunkRadius;
    public int TotalInstances => _totalInstances;
}

// Classe utilitaire pour le debugging
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
}