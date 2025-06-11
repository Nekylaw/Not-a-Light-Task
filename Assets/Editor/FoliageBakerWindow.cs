using Game.Services.LightSources;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using static FoliageVolume;

public class FoliageBakerWindow : EditorWindow
{
    [System.Serializable]
    public class BakeSettings
    {
        [Header("Chunk Configuration")]
        public Vector2Int chunkSize = new Vector2Int(16, 16);
        public string outputFolder = "Resources/FoliageChunks/";

        [Header("Performance")]
        public int maxInstancesPerFrame = 1000;
        public bool useMultithreading = true;
        public bool generatePreview = true;

        [Header("Quality")]
        public float raycastStartHeight = 100f;
        public float raycastMaxDistance = 200f;
        public bool alignToSurface = true;
        public float maxSlopeAngle = 45f;

        [Header("Optimization")]
        public bool mergeNearbyInstances = false;
        public float mergeDistance = 0.5f;
        public bool removeUndergroundInstances = true;
        public float undergroundThreshold = -0.1f;

        [Header("Debug")]
        public bool showProgress = true;
        public bool detailedLogging = false;
        public bool validateResults = true;
    }

    private BakeSettings _settings = new BakeSettings();
    private SerializedObject _serializedObject;
    private Dictionary<string, ChunkData> _chunks;

    // Baking state
    private bool _isBaking = false;
    private float _bakingProgress = 0f;
    private string _currentBakingStep = "";
    private int _totalInstances = 0;
    private int _processedInstances = 0;
    private System.DateTime _bakingStartTime;

    // Preview
    private bool _showPreview = false;
    private Vector2 _scrollPosition;
    private GUIStyle _headerStyle;
    private GUIStyle _progressStyle;

    [MenuItem("Window/Game/Foliage Baker")]
    public static void ShowWindow()
    {
        FoliageBakerWindow window = GetWindow<FoliageBakerWindow>("Foliage Baker");
        //window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);
        _chunks = new Dictionary<string, ChunkData>();
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        _progressStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f)) }
        };
    }

    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = color;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawHeader();
        DrawSettings();
        DrawBakingControls();
        DrawProgress();
        DrawPreview();
        DrawStatistics();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Generate optimized foliage chunks for runtime streaming", EditorStyles.helpBox);
        EditorGUILayout.Space(10);
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        _serializedObject.Update();

        // Chunk Configuration
        EditorGUILayout.LabelField("Chunk Configuration", EditorStyles.miniBoldLabel);
        _settings.chunkSize = EditorGUILayout.Vector2IntField("Chunk Size", _settings.chunkSize);
        _settings.outputFolder = EditorGUILayout.TextField("Output Folder", _settings.outputFolder);

        EditorGUILayout.Space(5);

        // Performance
        EditorGUILayout.LabelField("Performance", EditorStyles.miniBoldLabel);
        _settings.maxInstancesPerFrame = EditorGUILayout.IntSlider("Max Instances/Frame", _settings.maxInstancesPerFrame, 100, 5000);
        _settings.useMultithreading = EditorGUILayout.Toggle("Use Multithreading", _settings.useMultithreading);
        _settings.generatePreview = EditorGUILayout.Toggle("Generate Preview", _settings.generatePreview);

        EditorGUILayout.Space(5);

        // Quality
        EditorGUILayout.LabelField("Quality", EditorStyles.miniBoldLabel);
        _settings.raycastStartHeight = EditorGUILayout.FloatField("Raycast Start Height", _settings.raycastStartHeight);
        _settings.raycastMaxDistance = EditorGUILayout.FloatField("Raycast Max Distance", _settings.raycastMaxDistance);
        _settings.alignToSurface = EditorGUILayout.Toggle("Align To Surface", _settings.alignToSurface);
        if (_settings.alignToSurface)
        {
            _settings.maxSlopeAngle = EditorGUILayout.Slider("Max Slope Angle", _settings.maxSlopeAngle, 0f, 90f);
        }

        EditorGUILayout.Space(5);

        // Optimization
        EditorGUILayout.LabelField("Optimization", EditorStyles.miniBoldLabel);
        _settings.mergeNearbyInstances = EditorGUILayout.Toggle("Merge Nearby Instances", _settings.mergeNearbyInstances);
        if (_settings.mergeNearbyInstances)
        {
            _settings.mergeDistance = EditorGUILayout.FloatField("Merge Distance", _settings.mergeDistance);
        }
        _settings.removeUndergroundInstances = EditorGUILayout.Toggle("Remove Underground", _settings.removeUndergroundInstances);
        if (_settings.removeUndergroundInstances)
        {
            _settings.undergroundThreshold = EditorGUILayout.FloatField("Underground Threshold", _settings.undergroundThreshold);
        }

        EditorGUILayout.Space(5);

        // Debug
        _settings.showProgress = EditorGUILayout.Toggle("Show Progress", _settings.showProgress);
        _settings.detailedLogging = EditorGUILayout.Toggle("Detailed Logging", _settings.detailedLogging);
        _settings.validateResults = EditorGUILayout.Toggle("Validate Results", _settings.validateResults);

        _serializedObject.ApplyModifiedProperties();
    }

    private void DrawBakingControls()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Baking Controls", EditorStyles.boldLabel);

        FoliageVolume[] volumes = GameObject.FindObjectsByType<FoliageVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (volumes.Length == 0)
        {
            EditorGUILayout.HelpBox("No FoliageVolume objects found in the scene. Add some volumes to bake foliage.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Found {volumes.Length} foliage volume(s) in scene");

        GUI.enabled = !_isBaking;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bake All", GUILayout.Height(30)))
        {
            StartBaking();
        }

        if (GUILayout.Button("Clear Existing", GUILayout.Height(30)))
        {
            ClearExistingChunks();
        }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = true;

        if (_isBaking && GUILayout.Button("Cancel Baking", GUILayout.Height(25)))
        {
            CancelBaking();
        }
    }

    private void DrawProgress()
    {
        if (!_isBaking || !_settings.showProgress) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Baking Progress", EditorStyles.boldLabel);

        EditorGUILayout.LabelField(_currentBakingStep);

        Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressRect, _bakingProgress, $"{_processedInstances}/{_totalInstances} instances");

        var elapsed = System.DateTime.Now - _bakingStartTime;
        EditorGUILayout.LabelField($"Elapsed: {elapsed:mm\\:ss}");

        if (_bakingProgress > 0)
        {
            var estimated = System.TimeSpan.FromTicks((long)(elapsed.Ticks / _bakingProgress));
            var remaining = estimated - elapsed;
            if (remaining.TotalSeconds > 0)
            {
                EditorGUILayout.LabelField($"Estimated remaining: {remaining:mm\\:ss}");
            }
        }
    }

    private void DrawPreview()
    {
        if (_chunks.Count == 0) return;

        EditorGUILayout.Space(10);
        _showPreview = EditorGUILayout.Foldout(_showPreview, $"Preview ({_chunks.Count} chunks)", true);

        if (!_showPreview) return;

        foreach (var chunk in _chunks.Take(10))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Chunk: {chunk.Key}", EditorStyles.boldLabel);

            if (chunk.Value.MeshDatas != null)
            {
                foreach (var meshData in chunk.Value.MeshDatas)
                {
                    EditorGUILayout.LabelField($"  {meshData.MeshName}: {meshData.Matrices?.Count ?? 0} instances");
                }
            }

            EditorGUILayout.EndVertical();
        }

        if (_chunks.Count > 10)
        {
            EditorGUILayout.LabelField($"... and {_chunks.Count - 10} more chunks");
        }
    }

    private void DrawStatistics()
    {
        if (_chunks.Count == 0) return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        int totalInstances = _chunks.Values.Sum(c => c.MeshDatas?.Sum(m => m.Matrices?.Count ?? 0) ?? 0);
        int uniqueMeshes = _chunks.Values.SelectMany(c => c.MeshDatas ?? new List<MeshData>())
                                        .Select(m => m.MeshName)
                                        .Distinct()
                                        .Count();

        EditorGUILayout.LabelField($"Total Chunks: {_chunks.Count}");
        EditorGUILayout.LabelField($"Total Instances: {totalInstances:N0}");
        EditorGUILayout.LabelField($"Unique Meshes: {uniqueMeshes}");
        EditorGUILayout.LabelField($"Avg Instances/Chunk: {(totalInstances / Mathf.Max(1, _chunks.Count)):F1}");
    }

    private async void StartBaking()
    {
        _isBaking = true;
        _bakingProgress = 0f;
        _bakingStartTime = System.DateTime.Now;
        _processedInstances = 0;
        _totalInstances = 0;
        _chunks.Clear();

        try
        {
            await BakeAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Baking failed: {e.Message}");
            EditorUtility.DisplayDialog("Baking Failed", $"An error occurred during baking:\n{e.Message}", "OK");
        }
        finally
        {
            _isBaking = false;
            _currentBakingStep = "";
            Repaint();
        }
    }

    private async Task BakeAsync()
    {
        _currentBakingStep = "Analyzing volumes...";
        Repaint();

        FoliageVolume[] volumes = GameObject.FindObjectsByType<FoliageVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // Calculate total instances
        foreach (var volume in volumes)
        {
            Vector3 size = volume.VolumeSize;
            foreach (var model in volume.FoliageModels)
            {
                if (model.density > 0)
                {
                    _totalInstances += Mathf.FloorToInt(size.x * size.z * model.density);
                }
            }
        }

        if (_settings.detailedLogging)
            Debug.Log($"Starting bake of {_totalInstances} total instances across {volumes.Length} volumes");

        int processedInCurrentFrame = 0;

        foreach (var volume in volumes)
        {
            await ProcessVolumeAsync(volume, processedInCurrentFrame);
        }

        if (_settings.mergeNearbyInstances)
        {
            _currentBakingStep = "Merging nearby instances...";
            Repaint();
            await MergeNearbyInstancesAsync();
        }

        if (_settings.validateResults)
        {
            _currentBakingStep = "Validating results...";
            Repaint();
            ValidateResults();
        }

        _currentBakingStep = "Saving to files...";
        Repaint();
        SaveToJson();

        _bakingProgress = 1f;
        _currentBakingStep = "Baking completed!";

        var elapsed = System.DateTime.Now - _bakingStartTime;
        Debug.Log($"Baking completed in {elapsed:mm\\:ss}. Generated {_chunks.Count} chunks with {_processedInstances} instances.");

        if (_settings.generatePreview)
        {
            Repaint();
        }
    }

    private async Task ProcessVolumeAsync(FoliageVolume volume, int processedInCurrentFrame)
    {
        Vector3 size = volume.VolumeSize;
        Vector3 origin = volume.transform.position;
        LayerMask groundMask = volume.GroundMask;

        foreach (var model in volume.FoliageModels)
        {
            Mesh mesh = GetMeshFromModel(model);
            if (mesh == null || model.density <= 0) continue;

            int instanceCount = Mathf.FloorToInt(size.x * size.z * model.density);
            Random.InitState(volume.GetInstanceID() ^ mesh.name.GetHashCode());

            _currentBakingStep = $"Processing {mesh.name} in {volume.name}...";

            for (int i = 0; i < instanceCount; i++)
            {
                if (processedInCurrentFrame >= _settings.maxInstancesPerFrame)
                {
                    processedInCurrentFrame = 0;
                    _bakingProgress = (float)_processedInstances / _totalInstances;
                    Repaint();
                    await Task.Yield(); // Yield control back to Unity thread
                }

                if (!_isBaking) return; // Check for cancellation

                Vector3 localPos = new Vector3(
                    Random.Range(0f, size.x),
                    0f,
                    Random.Range(0f, size.z)
                );

                Vector3 worldXZPos = origin + localPos;
                Ray ray = new Ray(worldXZPos + Vector3.up * _settings.raycastStartHeight, Vector3.down);

                if (!Physics.Raycast(ray, out RaycastHit hit, _settings.raycastMaxDistance, groundMask))
                {
                    processedInCurrentFrame++;
                    _processedInstances++;
                    continue;
                }

                // Check angle
                if (_settings.alignToSurface)
                {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if (angle > _settings.maxSlopeAngle)
                    {
                        processedInCurrentFrame++;
                        _processedInstances++;
                        continue;
                    }
                }

                Vector3 finalPos = hit.point;

                // Remove underground instances
                if (_settings.removeUndergroundInstances && finalPos.y < _settings.undergroundThreshold)
                {
                    processedInCurrentFrame++;
                    _processedInstances++;
                    continue;
                }

                Quaternion rotation = _settings.alignToSurface ?
                    Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, Random.Range(model.rotationYRange.x, model.rotationYRange.y), 0) :
                    Quaternion.Euler(0, Random.Range(model.rotationYRange.x, model.rotationYRange.y), 0);

                float scale = Random.Range(model.scaleRange.x, model.scaleRange.y);
                Matrix4x4 matrix = Matrix4x4.TRS(finalPos, rotation, Vector3.one * scale);

                AddInstanceToChunk(matrix, mesh);

                processedInCurrentFrame++;
                _processedInstances++;
            }
        }
    }

    private Mesh GetMeshFromModel(FoliageModel model)
    {
        if (model.prefabSource?.GetComponent<MeshFilter>()?.sharedMesh != null)
            return model.prefabSource.GetComponent<MeshFilter>().sharedMesh;
        return null;
    }

    private void AddInstanceToChunk(Matrix4x4 matrix, Mesh mesh)
    {
        Vector3 pos = matrix.GetColumn(3);
        int chunkX = Mathf.FloorToInt(pos.x / _settings.chunkSize.x);
        int chunkY = Mathf.FloorToInt(pos.z / _settings.chunkSize.y);
        string chunkKey = $"{chunkX}_{chunkY}";

        if (!_chunks.ContainsKey(chunkKey))
        {
            _chunks[chunkKey] = new ChunkData
            {
                ChunkName = chunkKey,
                MeshDatas = new List<MeshData>()
            };
        }

        var meshDatasList = _chunks[chunkKey].MeshDatas;
        var meshData = meshDatasList.FirstOrDefault(md => md.MeshName == mesh.name);

        if (meshData == null)
        {
            meshData = new MeshData
            {
                MeshName = mesh.name,
                Matrices = new List<SerializableMatrix4x4>()
            };
            meshDatasList.Add(meshData);
        }

        meshData.Matrices.Add(new SerializableMatrix4x4(matrix));
    }

    private async Task MergeNearbyInstancesAsync()
    {
        await Task.Yield();
    }

    private void ValidateResults()
    {
        int totalInstances = _chunks.Values.Sum(c => c.MeshDatas?.Sum(m => m.Matrices?.Count ?? 0) ?? 0);

        if (totalInstances == 0)
        {
            Debug.LogWarning("No instances were generated during baking!");
        }

        var emptyChunks = _chunks.Where(c => c.Value.MeshDatas?.Count == 0).ToList();
        if (emptyChunks.Count > 0)
        {
            Debug.LogWarning($"Found {emptyChunks.Count} empty chunks");
        }
    }

    private void SaveToJson()
    {
        string path = Path.Combine(Application.dataPath, _settings.outputFolder);
        Directory.CreateDirectory(path);

        ClearBakeFiles(path);

        foreach (var chunk in _chunks)
        {
            string json = JsonUtility.ToJson(chunk.Value, true);
            string filePath = Path.Combine(path, $"{chunk.Key}.json");
            File.WriteAllText(filePath, json);
        }

        Debug.Log($"Baked {_chunks.Count} chunks to {path}");
        AssetDatabase.Refresh();
    }

    private void ClearBakeFiles(string path)
    {
        if (!Directory.Exists(path)) return;

        var files = Directory.GetFiles(path, "*.json");
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }

    private void ClearExistingChunks()
    {
        if (EditorUtility.DisplayDialog("Clear Chunks", "This will delete all existing chunk files. Continue?", "Yes", "No"))
        {
            string path = Path.Combine(Application.dataPath, _settings.outputFolder);
            ClearBakeFiles(path);
            AssetDatabase.Refresh();
            Debug.Log("Cleared existing chunk files");
        }
    }

    private void CancelBaking()
    {
        _isBaking = false;
        Debug.Log("Baking cancelled by user");
    }
}