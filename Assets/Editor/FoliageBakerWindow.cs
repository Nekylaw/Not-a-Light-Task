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

    /// <summary>
    /// Serialized object for the window
    /// </summary>
    private SerializedObject _serializedObject = null;

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
        _chunks = new Dictionary<string, ChunkData>();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

        EditorGUILayout.Space();
        _chunckSize = EditorGUILayout.Vector2IntField("Chunk Size", _chunckSize);

        _serializedObject.Update();
        _serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake"))
            Bake();
    }

    private void Bake()
    {
        _chunks.Clear();

        FoliageVolume[] volumes = GameObject.FindObjectsByType<FoliageVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var volume in volumes)
        {
            Vector3 size = volume.VolumeSize;
            Vector3 origin = volume.transform.position;
            LayerMask groundMask = volume.GroundMask;

            foreach (var model in volume.FoliageModels)
            {
                Mesh mesh = null;
                if (model.prefabSource != null)
                {
                    var mf = model.prefabSource.GetComponent<MeshFilter>();
                    if (mf != null)
                        mesh = mf.sharedMesh;
                }

                if (mesh == null || model.density <= 0)
                    continue;

                int instanceCount = Mathf.FloorToInt(size.x * size.z * model.density);
                Random.InitState(volume.GetInstanceID() ^ mesh.name.GetHashCode());

                for (int i = 0; i < instanceCount; i++)
                {
                    Vector3 localPos = new Vector3(
                        Random.Range(0f, size.x),
                        0f,
                        Random.Range(0f, size.z)
                    );

                    Vector3 worldXZPos = origin + localPos;

                    Ray ray = new Ray(worldXZPos + Vector3.up * 100f, Vector3.down);

                    if (!Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
                        continue;

                    Vector3 finalPos = hit.point;

                    Quaternion rotation = Quaternion.Euler(0, Random.Range(model.rotationYRange.x, model.rotationYRange.y), 0);
                    float scale = Random.Range(model.scaleRange.x, model.scaleRange.y);
                    Matrix4x4 matrix = Matrix4x4.TRS(finalPos, rotation, Vector3.one * scale);

                    int chunkX = Mathf.FloorToInt(finalPos.x / _chunckSize.x);
                    int chunkY = Mathf.FloorToInt(finalPos.z / _chunckSize.y);
                    string chunkKey = $"{chunkX}_{chunkY}";

                    if (!_chunks.ContainsKey(chunkKey))
                    {
                        _chunks[chunkKey] = new ChunkData
                        {
                            ChunkName = chunkKey,
                            MeshDatas = new List<MeshData>()
                        };
                    }

                    List<MeshData> meshDatasList = _chunks[chunkKey].MeshDatas;

                    MeshData meshData = meshDatasList.Find(md => md.MeshName == mesh.name);
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