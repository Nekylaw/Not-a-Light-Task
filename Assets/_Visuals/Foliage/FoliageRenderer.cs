using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class FoliageRenderer : MonoBehaviour
{
    [SerializeField] private string _jsonFileName = "GrassBunch";
    [SerializeField] private Material _material;
    [SerializeField] private float _renderDistance = 100f;
    [SerializeField] private float _cullDistance = 150f;

    private List<Mesh> _meshes = new();
    private List<int> _meshStartIndices = new();
    private List<Vector3> _meshCenters = new();
    private List<ComputeBuffer> _argsBuffers = new();

    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _baseScaleBuffer;

    private Transform _player;

    struct DrawData
    {
        public Matrix4x4 matrix;
        public Vector4 baseScale;
    }

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

        List<DrawData> drawData = new();

        foreach (var entry in data.MeshDatas)
        {
            Mesh mesh = FindMeshByName(entry.MeshName);

            if (mesh == null)
            {
                Debug.LogWarning("Mesh not found: " + entry.MeshName);
                continue;
            }

            _meshes.Add(mesh);
            _meshStartIndices.Add(drawData.Count);

            Vector3 avgCenter = Vector3.zero;
            List<Matrix4x4> entryMatrices = new();

            foreach (var mat in entry.Matrices)
            {
                Matrix4x4 matrix = mat.ToMatrix();
                Vector3 lossyScale = matrix.lossyScale;

                drawData.Add(new DrawData
                {
                    matrix = matrix,
                    baseScale = new Vector4(lossyScale.x, lossyScale.y, lossyScale.z, 1f)
                });

                entryMatrices.Add(matrix);
                avgCenter += (Vector3)matrix.GetColumn(3);
            }

            avgCenter /= Mathf.Max(1, entryMatrices.Count);
            _meshCenters.Add(avgCenter);
        }

        if (drawData.Count == 0)
        {
            Debug.LogWarning("No instances to render.");
            return;
        }

        _matrixBuffer = new ComputeBuffer(drawData.Count, sizeof(float) * 16);
        _baseScaleBuffer = new ComputeBuffer(drawData.Count, sizeof(float) * 4);

        _matrixBuffer.SetData(drawData.Select(d => d.matrix).ToArray());
        _baseScaleBuffer.SetData(drawData.Select(d => d.baseScale).ToArray());

        _material.SetBuffer("_Matrices", _matrixBuffer);
        _material.SetBuffer("_BaseScale", _baseScaleBuffer);
        _material.enableInstancing = true;

        for (int i = 0; i < _meshes.Count; i++)
        {
            Mesh mesh = _meshes[i];
            int start = _meshStartIndices[i];
            int count = (i + 1 < _meshStartIndices.Count) ? _meshStartIndices[i + 1] - start : drawData.Count - start;

            uint[] args = new uint[5]
            {
                mesh.GetIndexCount(0),
                (uint)count,
                mesh.GetIndexStart(0),
                mesh.GetBaseVertex(0),
                0
            };

            ComputeBuffer argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            _argsBuffers.Add(argsBuffer);
        }
    }

    private void Update()
    {
        if (_player == null || _matrixBuffer == null) return;

        _material.SetVector("_PlayerPos", _player.position);

        float maxDistSqr = _cullDistance * _cullDistance;

        for (int i = 0; i < _meshes.Count; i++)
        {
            Vector3 center = _meshCenters[i];
            float distSqr = (_player.position - center).sqrMagnitude;

            //if (distSqr > maxDistSqr)
            //    continue; // Cull distant groups

            Mesh mesh = _meshes[i];
            ComputeBuffer argsBuffer = _argsBuffers[i];
            int offset = _meshStartIndices[i];

            _material.SetInt("_MatrixOffset", offset);

            Graphics.DrawMeshInstancedIndirect(
                mesh,
                0,
                _material,
                new Bounds(center, Vector3.one * (_renderDistance * 2f)), // tighter bound
                argsBuffer,
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

        foreach (var buffer in _argsBuffers)
            buffer?.Release();

        _argsBuffers.Clear();
    }

    Mesh FindMeshByName(string name)
    {
        Mesh[] allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
        foreach (var m in allMeshes)
        {
            if (m.name == name)
                return m;
        }

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
