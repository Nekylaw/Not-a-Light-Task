using Codice.Utils;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FoliageBakerWindow : EditorWindow
{

    #region Sub-Classes 

    /// <summary>
    /// Represents a group of a foliage matrix datas.
    /// </summary>
    [System.Serializable]
    class FoliageMatrixData
    {
        public string SaveName;
        public List<SerializableMatrix4x4> Matrices;
    }

    /// <summary>
    /// Represents a single foliage instance's spatial coordinates as a serialisable matrix.
    /// </summary>
    [System.Serializable]
    struct SerializableMatrix4x4
    {
        public float[] values;

        public SerializableMatrix4x4(Matrix4x4 m)
        {
            values = new float[16];
            for (int i = 0; i < 16; i++)
                values[i] = m[i];
        }

        public Matrix4x4 ToMatrix()
        {
            Matrix4x4 m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
                m[i] = values[i];
            return m;
        }
    }

    #endregion


    private string _saveFile = "GrassMatrices.json";
    private string _dataSaveName = "GrassMatrices";
    private Mesh _mesh = null;
    private Material _material = null;

    private List<Matrix4x4> _matrices = new();
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
        GUILayout.Label("Data settings", EditorStyles.boldLabel);

        _mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", _mesh, typeof(Mesh), false);
        _material = (Material)EditorGUILayout.ObjectField("Material", _material, typeof(Material), false);
        _saveFile = EditorGUILayout.TextField("Save Path", _saveFile);

        EditorGUILayout.HelpBox("1 bake = 1 file. If a file name already exists, it will be REPLACED.", MessageType.Warning);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Bake"))
            BakeMatrices();

        if(_hasBaked)
        EditorGUILayout.HelpBox("Bake done.", MessageType.Info);

        // Display saved datas.
        string preview = GetDataInfos();
        if (!string.IsNullOrEmpty(preview))
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(preview, MessageType.Info);

            EditorGUILayout.Space(20);
        }

        if (GUILayout.Button("Save"))
            SaveToJson(Path.Combine("Resources/", _saveFile));
    }

    private void BakeMatrices()
    {

        if (_mesh == null || string.IsNullOrEmpty(_saveFile))
        {
            Debug.LogError("Missing mesh or save path", this);
            return;
        }

        _matrices.Clear();

        foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (mf == null || mf.sharedMesh != _mesh)
                continue;

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || _material != null && mr.sharedMaterial != _material) // ensure that the mesh has the choosen material if it exists
                continue;

            _matrices.Add(mf.transform.localToWorldMatrix);

            List<SerializableMatrix4x4> serializedMatrices = new();
            foreach (var m in _matrices)
            {
                SerializableMatrix4x4 serilizedMatrix = new SerializableMatrix4x4(m);
                serializedMatrices.Add(serilizedMatrix);
            }

            _data.Matrices = serializedMatrices;
            _data.SaveName = Path.GetFileNameWithoutExtension(_saveFile);
        }

        _hasBaked = true;
    }

    private void SaveToJson(string savePath)
    {
        if (!_hasBaked)
        {
            Debug.LogWarning("You have to bake matrices before save.");
            return;
        }

        string json = JsonUtility.ToJson(_data, true);
        string path = Path.Combine(Application.dataPath, savePath);
        File.WriteAllText(path, json);
        Debug.Log("Saved foliage matrix data at " + Path.Combine(Application.dataPath, savePath));

        AssetDatabase.Refresh();

        _hasBaked = false;
    }


    private string GetDataInfos()
    {
        string assetPath = Path.Combine("Assets/", "Resources/", _saveFile);
        if (!File.Exists(assetPath))
            return "No datas exists in this file.";

        string json = File.ReadAllText(assetPath);
        FoliageMatrixData data = JsonUtility.FromJson<FoliageMatrixData>(json);

        return $"Nom: {data.SaveName} | Matrices: {data.Matrices.Count}";
    }

}
