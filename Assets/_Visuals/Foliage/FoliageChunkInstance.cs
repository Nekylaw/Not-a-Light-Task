using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public class FoliageChunkInstance
{
    private List<Mesh> _meshes = new();
    private List<int> _startOffsets = new();
    private List<Vector3> _centers = new();
    private List<ComputeBuffer> _argsBuffers = new();
    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _scaleBuffer;
    private Material _material;

    public FoliageChunkInstance(ChunkData chunk, Material material)
    {
        _material = material;
        List<Matrix4x4> matrices = new();
        List<Vector4> scales = new();

        foreach (var meshData in chunk.MeshDatas)
        {
            Mesh mesh = Resources.FindObjectsOfTypeAll<Mesh>().FirstOrDefault(m => m.name == meshData.MeshName);
            if (!mesh) continue;

            _meshes.Add(mesh);
            _startOffsets.Add(matrices.Count);

            Vector3 center = Vector3.zero;
            foreach (var matrice in meshData.Matrices)
            {
                Matrix4x4 m = matrice.ToMatrix();
                Vector3 scale = m.lossyScale;
                scales.Add(new Vector4(scale.x, scale.y, scale.z, 1f));
                matrices.Add(m);

                center += (Vector3)m.GetColumn(3);
            }

            _centers.Add(center / Mathf.Max(1, meshData.Matrices.Count));
        }

        _matrixBuffer = new ComputeBuffer(matrices.Count, sizeof(float) * 16);
        _matrixBuffer.SetData(matrices);

        _scaleBuffer = new ComputeBuffer(scales.Count, sizeof(float) * 4);
        _scaleBuffer.SetData(scales);

        _material.SetBuffer("_Matrices", _matrixBuffer);
        _material.SetBuffer("_BaseScales", _scaleBuffer);

        CreateArgsBuffers(matrices.Count);
    }

    private void CreateArgsBuffers(int totalCount)
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            Mesh mesh = _meshes[i];
            int start = _startOffsets[i];
            int count = (i + 1 < _startOffsets.Count) ? _startOffsets[i + 1] - start : totalCount - start;

            uint[] args = new uint[5] {
                mesh.GetIndexCount(0),
                (uint)count,
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0
            };

            ComputeBuffer argsBuf = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuf.SetData(args);
            _argsBuffers.Add(argsBuf);
        }
    }

    public void Draw(Vector3 playerPos, float renderDist, float cullDist)
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            if ((playerPos - _centers[i]).sqrMagnitude > cullDist * cullDist)
                continue;

            _material.SetInt("_MatrixOffset", _startOffsets[i]);
            _material.SetFloat("_FlowTime", Time.time);

            Graphics.DrawMeshInstancedIndirect(
                _meshes[i],
                0,
                _material,
                new Bounds(_centers[i], Vector3.one * (renderDist * 2f)),
                _argsBuffers[i],
                0,
                null,
                ShadowCastingMode.Off,
                false,
                0,
                null,
                LightProbeUsage.Off
            );
        }

        //Debug.Log($"Drawn {_meshes.Count} meshes with {_argsBuffers.Count} args buffers.");
    }

    public void Dispose()
    {
        _matrixBuffer?.Release();
        _scaleBuffer?.Release();
        foreach (var buf in _argsBuffers)
            buf?.Release();
        _argsBuffers.Clear();
    }
}
