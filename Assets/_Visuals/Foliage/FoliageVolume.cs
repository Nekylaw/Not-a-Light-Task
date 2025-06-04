using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a volume in which foliage models can be placed randomly. Used to define areas for foliage generation in the scene.
/// </summary>
[ExecuteAlways]
public class FoliageVolume : MonoBehaviour
{

    #region Subclasses  

    /// <summary>
    /// Represents a foliage model with its prefab source, density, scale range, and rotation range to be used in the foliage volume.
    /// </summary>
    [System.Serializable]
    public struct FoliageModel
    {
        public GameObject prefabSource;
        public float density;
        public Vector2 scaleRange;
        public Vector2 rotationYRange;
    }

    #endregion

    #region Fields

    [SerializeField]
    private Vector3 volumeSize = new Vector3(2, 0.5f, 2);

    [SerializeField] 
    private LayerMask groundMask = ~0;

    [SerializeField]
    private List<FoliageModel> _foliageModels = new();

    #endregion

    #region Public API

    public List<FoliageModel> FoliageModels => _foliageModels;
    public Vector3 VolumeSize => volumeSize;
    public LayerMask GroundMask => groundMask;

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + volumeSize * 0.5f, volumeSize);

        Gizmos.color = Color.yellow;

        foreach (var model in _foliageModels)
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

            int estimatedCount = Mathf.FloorToInt(volumeSize.x * volumeSize.z * model.density);
            Random.InitState(0); // constant random seed

            for (int i = 0; i < estimatedCount; i++)
            {
                Vector3 randomPos = transform.position + new Vector3(
                    Random.Range(0, volumeSize.x),
                    0,
                    Random.Range(0, volumeSize.z)
                );

                Quaternion rot = Quaternion.Euler(0, Random.Range(model.rotationYRange.x, model.rotationYRange.y), 0);
                float scale = Random.Range(model.scaleRange.x, model.scaleRange.y);
                Matrix4x4 matrix = Matrix4x4.TRS(randomPos, rot, Vector3.one * scale);

#if UNITY_EDITOR
                if (UnityEditor.SceneView.lastActiveSceneView != null)
                {
                    Gizmos.matrix = matrix;
                    Gizmos.DrawWireMesh(mesh);
                }
#endif
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    #endregion
}
