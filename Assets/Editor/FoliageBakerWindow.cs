using Game.Services.LightSources;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Mesh;

public class FoliageBakerWindow : EditorWindow
{

    /// <summary>
    /// Size of the chunk in world units
    /// </summary>
    private Vector2Int _chunckSize = new Vector2Int(100, 100);

    /// <summary>
    /// Folder where the baked foliage chunks will be saved
    /// </summary>
    private string _outputFolder = "Resources/FoliageChunks/";

    [Tooltip("List of GameObjects that are considered foliage sources.")]
    [SerializeField]
    private List<GameObject> _foliageSources = null;

    /// <summary>
    /// Serialized object for the window
    /// </summary>
    private SerializedObject _serializedObject = null;

    /// <summary>
    /// Serialized property for the foliage sources list
    /// </summary>
    private SerializedProperty _foliageSourcesProperty = null;

    /// <summary>
    /// Chunks data with key as "chunkX_chunkY" and value as ChunkData.
    /// </summary>
    Dictionary<string, ChunkData> _chunks;


    [MenuItem("Window/Game/Foliage Baker")]
    public static void ShowWindow()
    {
        FoliageBakerWindow window = GetWindow<FoliageBakerWindow>("Foliage Baker");
        window.Show();
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this); // Window as serialized object
        _foliageSourcesProperty = _serializedObject.FindProperty($"{nameof(_foliageSources)}");

        _chunks = new Dictionary<string, ChunkData>();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
        EditorGUILayout.Space();
        _chunckSize = EditorGUILayout.Vector2IntField("Chunk Size", _chunckSize);
        _serializedObject.Update();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_foliageSourcesProperty, true);
        _serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Bake Scene Foliage"))
            Bake();
    }

    private void Bake()
    {
        if (_foliageSources.Count <= 0)
            return;

        _chunks.Clear();

        foreach (var obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(obj);

            if (source == null || !_foliageSources.Contains(source))
                continue;

            foreach (var mf in obj.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null)
                    continue;

                Vector3 meshPos = mf.transform.position;
                int chunkX = Mathf.FloorToInt(meshPos.x / _chunckSize.x);
                int chunkY = Mathf.FloorToInt(meshPos.y / _chunckSize.y);
                string chunkKey = $"{chunkX}_{chunkY}";

                // Create or get existing chunk data
                if (!_chunks.ContainsKey(chunkKey))
                {
                    _chunks[chunkKey] = new ChunkData
                    {
                        ChunkName = chunkKey,
                        MeshDatas = new List<MeshData>()
                    };
                }

                List<MeshData> meshDatasList = _chunks[chunkKey].MeshDatas;
                MeshData meshData = _chunks[chunkKey].MeshDatas.Find(md => md.MeshName == mesh.name);
                if (meshData == null)
                {
                    meshData = new MeshData { MeshName = mesh.name, Matrices = new List<SerializableMatrix4x4>() };
                    meshDatasList.Add(meshData);
                }

                meshData.Matrices.Add(new SerializableMatrix4x4(mf.transform.localToWorldMatrix));
            }
        }

        SaveToJson();
    }


    private void SaveToJson()
    {
        string path = Path.Combine(Application.dataPath, _outputFolder); // Assets + /Resources/FoliageChunks/
        Directory.CreateDirectory(path); // Ensure the directory exists

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
        var files = Directory.GetFiles(path);
        for (int i = 0; i < files.Length; i++)
        {
            File.Delete(files[i]);
        }
    }
}