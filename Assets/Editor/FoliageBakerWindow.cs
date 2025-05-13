using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FoliageBakerWindow : EditorWindow
{
    private GameObject _foliageSourcePrefab = null;

    private string _saveFile = "GrassBunch.json";

    private FoliageMatrixData _data = new();
    private bool _hasBaked = false;

    [MenuItem("Window/Game/Foliage Baker")]
    public static void ShowWindow()
    {
        FoliageBakerWindow window = GetWindow<FoliageBakerWindow>("Foliage Baker");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Foliage Baker", EditorStyles.boldLabel);

        _foliageSourcePrefab = (GameObject)EditorGUILayout.ObjectField("Grass Source Prefab", _foliageSourcePrefab, typeof(GameObject), false);
        _saveFile = EditorGUILayout.TextField("Save Path", _saveFile);

        EditorGUILayout.HelpBox("Keep the same Save Path to replace json grass datas.", MessageType.Info);

        if (GUILayout.Button("Bake"))
            Bake();

        if (_hasBaked)
            EditorGUILayout.HelpBox("Bake done.", MessageType.Info);

        string preview = GetDataInfos();
        if (!string.IsNullOrEmpty(preview))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(preview, MessageType.None);
        }

        if (GUILayout.Button("Save"))
            SaveToJson(Path.Combine("Resources/", _saveFile));
    }

    private void Bake()
    {
        if (_foliageSourcePrefab == null)
        {
            Debug.LogError("No prefab assigned.");
            return;
        }

        GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(_foliageSourcePrefab);
        if (prefabSource == null)
        {
            Debug.LogError("The selected GameObject is not a prefab or prefab instance.");
            return;
        }

        Dictionary<Mesh, List<Matrix4x4>> matrixMap = new();

        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            GameObject goPrefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(go);

            if (goPrefabRoot == null)
                continue;

            if (goPrefabRoot != _foliageSourcePrefab)
                continue;

            Debug.Log($"[FOUND MATCH] {go.name} is instance of {_foliageSourcePrefab.name}");

            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null)
                    continue;

                Debug.Log($"   MeshFilter: {mf.name} | Mesh: {mf.sharedMesh.name}");

                Mesh mesh = mf.sharedMesh;
                if (!matrixMap.ContainsKey(mesh))
                    matrixMap[mesh] = new List<Matrix4x4>();

                matrixMap[mesh].Add(mf.transform.localToWorldMatrix);
            }
        }

        _data = new FoliageMatrixData
        {
            SaveName = Path.GetFileNameWithoutExtension(_saveFile),
            MeshDatas = new List<MeshData>()
        };

        foreach (var kvp in matrixMap)
        {
            var entry = new MeshData
            {
                MeshName = kvp.Key.name,
                Matrices = kvp.Value.ConvertAll(m => new SerializableMatrix4x4(m))
            };

            _data.MeshDatas.Add(entry);
        }

        _hasBaked = true;

        int totalInstances = 0;
        foreach (var meshData in _data.MeshDatas)
            totalInstances += meshData.Matrices.Count;

        Debug.Log($"Total instances to be saved: {totalInstances}");
    }

    private void SaveToJson(string savePath)
    {
        if (!_hasBaked)
        {
            Debug.LogWarning("Bake before saving.");
            return;
        }

        string json = JsonUtility.ToJson(_data, true);
        string path = Path.Combine(Application.dataPath, savePath);

        File.WriteAllText(path, json);

        Debug.Log("Saved foliage matrix data at: " + path);
        AssetDatabase.Refresh();
        _hasBaked = false;
    }

    private string GetDataInfos()
    {
        string assetPath = Path.Combine("Assets", "Resources", _saveFile);
        if (!File.Exists(assetPath))
            return "No data found";

        string json = File.ReadAllText(assetPath);
        FoliageMatrixData data = JsonUtility.FromJson<FoliageMatrixData>(json);

        if (data == null || data.MeshDatas == null)
            return "File invalid or empty.";

        string summary = $"Name: {data.SaveName} | Mesh Count: {data.MeshDatas.Count}";
        foreach (var mesh in data.MeshDatas)
            summary += $"\n- {mesh.MeshName}: {mesh.Matrices.Count} matrices";

        return summary;
    }
}
