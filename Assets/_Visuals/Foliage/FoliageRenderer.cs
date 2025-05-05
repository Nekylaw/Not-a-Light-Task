using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FoliageRenderer : MonoBehaviour
{
    [SerializeField] private string _jsonFileName = "GrassBunch";
    [SerializeField] private Material _material;
    [SerializeField] private int _spawnSurfaceRange = 250;

    private class FoliageMeshGroup
    {
        public Mesh mesh;
        public int offset;
        public int count;
        public ComputeBuffer argsBuffer;
    }

    private List<FoliageMeshGroup> _meshGroups = new();
    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _baseScaleBuffer;

    private Transform _player;
    private Bounds _globalBounds;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerController>()?.transform;
        if (_player == null)
        {
            Debug.LogError("Player not found in scene.");
            return;
        }

        TextAsset jsonAsset = Resources.Load<TextAsset>(_jsonFileName);
        if (jsonAsset == null)
        {
            Debug.LogError("JSON not found: " + _jsonFileName);
            return;
        }

        var data = JsonUtility.FromJson<FoliageMatrixData>(jsonAsset.text);
        if (data?.MeshDatas == null)
        {
            Debug.LogError("Invalid foliage data.");
            return;
        }

        List<Matrix4x4> matrices = new();
        List<Vector4> baseScales = new();

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (var entry in data.MeshDatas)
        {
            Mesh mesh = FindMeshByName(entry.MeshName);
            if (mesh == null)
            {
                Debug.LogWarning("Mesh not found: " + entry.MeshName);
                continue;
            }

            int offset = matrices.Count;
            foreach (var m in entry.Matrices)
            {
                var matrix = m.ToMatrix();
                var pos = matrix.GetColumn(3);
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);

                matrices.Add(matrix);
                var s = matrix.lossyScale;
                baseScales.Add(new Vector4(s.x, s.y, s.z, 1f));
            }

            int count = matrices.Count - offset;

            var args = new uint[5]
            {
                mesh.GetIndexCount(0),
                (uint)count,
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0
            };

            var argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            _meshGroups.Add(new FoliageMeshGroup
            {
                mesh = mesh,
                offset = offset,
                count = count,
                argsBuffer = argsBuffer
            });
        }

        if (matrices.Count == 0)
        {
            Debug.LogWarning("No foliage instances to render.");
            return;
        }

        _matrixBuffer = new ComputeBuffer(matrices.Count, sizeof(float) * 16);
        _baseScaleBuffer = new ComputeBuffer(baseScales.Count, sizeof(float) * 4);
        _matrixBuffer.SetData(matrices);
        _baseScaleBuffer.SetData(baseScales);

        _material.SetBuffer("_Matrices", _matrixBuffer);
        _material.SetBuffer("_BaseScale", _baseScaleBuffer);
        _material.enableInstancing = true;

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = (max - min) + Vector3.one * 10f;
        _globalBounds = new Bounds(center, size);
    }

    private void Update()
    {
        if (_matrixBuffer == null || _player == null) return;

        _material.SetVector("_PlayerPos", _player.position);

        foreach (var group in _meshGroups)
        {
            _material.SetInt("_MatrixOffset", group.offset);

            Graphics.DrawMeshInstancedIndirect(
                group.mesh,
                0,
                _material,
                _globalBounds,
                group.argsBuffer,
                0,
                null,
                ShadowCastingMode.On,
                true,
                0,
                null,
                LightProbeUsage.Off,
                null
            );
        }
    }

    private void OnDisable()
    {
        _matrixBuffer?.Release();
        _baseScaleBuffer?.Release();
        foreach (var g in _meshGroups)
            g.argsBuffer?.Release();
    }

    Mesh FindMeshByName(string name)
    {
        Mesh[] allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
        foreach (var m in allMeshes)
            if (m.name == name)
                return m;

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Mesh " + name);
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Mesh mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null && mesh.name == name)
                return mesh;
        }
#endif
        return null;
    }
}
