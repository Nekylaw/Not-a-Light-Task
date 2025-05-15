using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering;

public class FoliageController : MonoBehaviour
{
    private Transform _player;

    [Header("Grass")]

    [SerializeField]
    private string _jsonFileName = "GrassBunch";
    [SerializeField]
    private Material _material;
    [SerializeField]
    private float _renderDistance = 100f;
    [SerializeField]
    private float _cullDistance = 150f;

    private List<Mesh> _meshes = new();
    private List<int> _meshStartIndices = new();
    private List<Vector3> _meshCenters = new();
    private List<ComputeBuffer> _argsBuffers = new();
    private ComputeBuffer _matrixBuffer;
    private ComputeBuffer _baseScaleBuffer;

    [Header("Wind")]

    [SerializeField]
    private ComputeShader _windCompute;
    [SerializeField]
    private int _windMapSize = 128;
    [SerializeField]
    private float _windScale = 100f;
    [SerializeField]
    private float _windFrequency = 1;
    [SerializeField]
    private float _windAmplitude = 1;

    private RenderTexture _windMap;

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

        var drawData = LoadFoliageData();
        if (drawData == null || drawData.Count == 0)
            return;

        InitializeBuffers(drawData);
        CreateArgsBuffers(drawData);
        CreateWindRenderTexture();
    }

    private void Update()
    {
        if (_player == null || _matrixBuffer == null)
            return;

        _material.SetVector("_PlayerPos", _player.position);

        ComputeWind();

        for (int i = 0; i < _meshes.Count; i++)
        {
            Vector3 center = _meshCenters[i];
            float distSqr = (_player.position - center).sqrMagnitude;

            if (distSqr > _cullDistance * _cullDistance)
                continue;

            DrawGrass(i, center);
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

    /// <summary>
    /// Loads JSON foliage data and builds DrawData list
    /// </summary>
    private List<DrawData> LoadFoliageData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(_jsonFileName);
        if (jsonAsset == null)
        {
            Debug.LogError("JSON not found: " + _jsonFileName);
            return null;
        }

        var data = JsonUtility.FromJson<FoliageMatrixData>(jsonAsset.text);
        if (data?.MeshDatas == null)
        {
            Debug.LogError("Invalid foliage data.");
            return null;
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
                avgCenter += new Vector3(matrix.m03, matrix.m13, matrix.m23);
            }

            avgCenter /= Mathf.Max(1, entryMatrices.Count);
            _meshCenters.Add(avgCenter);
        }

        return drawData;
    }

    /// <summary>
    /// Initializes the GPU buffers with matrix and scale data
    /// </summary>
    private void InitializeBuffers(List<DrawData> drawData)
    {
        _matrixBuffer = new ComputeBuffer(drawData.Count, sizeof(float) * 16);
        _baseScaleBuffer = new ComputeBuffer(drawData.Count, sizeof(float) * 4);

        _matrixBuffer.SetData(drawData.Select(d => d.matrix).ToArray());
        _baseScaleBuffer.SetData(drawData.Select(d => d.baseScale).ToArray());

        _material.SetBuffer("_Matrices", _matrixBuffer);
        _material.SetBuffer("_BaseScale", _baseScaleBuffer);
        _material.enableInstancing = true;
    }

    /// <summary>
    /// Creates indirect draw argument buffers for each mesh group
    /// </summary>
    private void CreateArgsBuffers(List<DrawData> drawData)
    {
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

    /// <summary>
    /// Draw mesh with a single draw call for a single mesh group
    /// </summary>
    private void DrawGrass(int index, Vector3 center)
    {
        _material.SetInt("_MatrixOffset", _meshStartIndices[index]);

        Graphics.DrawMeshInstancedIndirect(
            _meshes[index],
            0,
            _material,
            new Bounds(center, Vector3.one * (_renderDistance * 2f)),
            _argsBuffers[index],
            0,
            null,
            ShadowCastingMode.Off,
            false,
            0,
            null,
            LightProbeUsage.Off,
            null
        );
    }

    /// <summary>
    /// Creates the wind render texture
    /// </summary>
    private void CreateWindRenderTexture()
    {
        _windMap = new RenderTexture(_windMapSize, _windMapSize, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Repeat
        };
        _windMap.Create();
    }

    /// <summary>
    /// Dispatches the wind compute shader to update the global wind map
    /// </summary>
    private void ComputeWind()
    {
        if (_windMap == null || !_windMap.IsCreated())
            CreateWindRenderTexture();

        int kernel = _windCompute.FindKernel("WindNoise");

        _windCompute.SetTexture(kernel, "_WindMap", _windMap);
        _windCompute.SetFloat("_Frequency", _windFrequency);
        _windCompute.SetFloat("_Amplitude", _windAmplitude);
        _windCompute.SetFloat("_Time", Time.time);
        _windCompute.SetFloat("_Scale", _windScale);

        int threadGroups = Mathf.Max(1, Mathf.CeilToInt(_windMapSize / 8.0f));
        _windCompute.Dispatch(kernel, threadGroups, threadGroups, 1);

        // Set the global texture for shaders using windmap
        Shader.SetGlobalTexture("_WindMap", _windMap);
    }

    /// <summary>
    /// Finds a mesh by name in memory or asset database (Editor-only fallback)
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private Mesh FindMeshByName(string name)
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
