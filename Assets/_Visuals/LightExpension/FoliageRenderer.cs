using UnityEngine;
using System.Collections.Generic;

public class FoliageRenderer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The foliage mesh")]
    private Mesh foliageMesh; //@todo use 3 meshes if billbording

    [SerializeField]
    [Tooltip("The foliage material")]
    private Material foliageMaterial;

    [SerializeField]
    [Tooltip("The maximum distance at which foliage is rendered")]
    public float renderDistance = 50f;

    [SerializeField]
    [Tooltip("Total number of foliage instances to generate. Called on Start \n Set ^2 values")]
    public int numberOfFoliage = 100000;

    /// <summary>
    /// The maximum number of foliage instances per batch
    /// </summary>
    private const int maxInstancesPerBatch = 1024;

    /// <summary>
    /// List to store the positions of the foliage instances
    /// </summary>
    private List<Vector3> foliagePositions = new List<Vector3>();

    /// <summary>
    /// List to store the rotations of the foliage instances
    /// </summary>
    private List<Quaternion> foliageRotations = new List<Quaternion>();

    /// <summary>
    /// List to store the scales of the foliage instances
    /// </summary>
    private List<Vector3> foliageScales = new List<Vector3>();

    /// <summary>
    /// List to store the visible foliage instances for rendering
    /// </summary>
    private List<Matrix4x4> visibleInstances = new List<Matrix4x4>();

    [SerializeField]
    private Transform player;

    [SerializeField]
    private Camera mainCamera;

    void Start()
    {
        GenerateFoliage();
        foliageMaterial.enableInstancing = true;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    /// <summary>
    /// Generates random foliage positions, rotations, and scales within a specified range.
    /// </summary>
    void GenerateFoliage()
    {
        foliagePositions.Clear();
        foliageRotations.Clear();
        foliageScales.Clear();

        // Generate random positions, rotations, and scales for the foliage instances
        for (int i = 0; i < numberOfFoliage; i++)
        {
            // Random positions within a -100 to 100 range on the X and Z axes
            //@todo use height map and terrain bounds
            Vector3 position = new Vector3(Random.Range(-100, 100), 0, Random.Range(-100, 100));
            foliagePositions.Add(position);

            // Random rotation around the Y-axis (vertical)
            Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            foliageRotations.Add(randomRotation);

            // Random scale between 0.5 and 1.5 times the original size
            float randomScale = Random.Range(0.5f, 1.5f);
            foliageScales.Add(Vector3.one * randomScale);
        }
    }

    void Update()
    {
        RenderFoliage();
    }

    /// <summary>
    /// Renders the foliage.
    /// </summary>
    private void RenderFoliage()
    {
        // Square of the render distance to avoid calculating square roots in each frame
        float sqrRenderDistance = renderDistance * renderDistance;

        visibleInstances.Clear();

        // Calculate the camera's frustum planes
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        // Loop through all the foliage positions to check visible ones
        for (int i = 0; i < foliagePositions.Count; i++)
        {
            Vector3 position = foliagePositions[i];
            Quaternion rotation = foliageRotations[i];
            Vector3 scale = foliageScales[i];

            // Check if the foliage is within the render distance
            if ((position - player.position).sqrMagnitude < sqrRenderDistance)
            {
                // Check if the foliage is within the camera's frustum (field of view)
                if (GeometryUtility.TestPlanesAABB(planes, new Bounds(position, Vector3.one))) //@todo use real bounds
                {
                    // Add the visible foliage instance to the matrix list with predefined rotation and scale
                    visibleInstances.Add(Matrix4x4.TRS(position, rotation, scale));
                }
            }
        }

        // Batch instances
        int batchCount = Mathf.CeilToInt((float)visibleInstances.Count / maxInstancesPerBatch);
        for (int i = 0; i < batchCount; i++)
        {
            int count = Mathf.Min(maxInstancesPerBatch, visibleInstances.Count - i * maxInstancesPerBatch);

            Matrix4x4[] batchArray = new Matrix4x4[count]; //Batch Matrices
            visibleInstances.CopyTo(i * maxInstancesPerBatch, batchArray, 0, count);  // Copy the instances into the batch array

            //Graphics.RenderMeshIndirect instead
            // GPU instancing
            Graphics.DrawMeshInstanced(foliageMesh, 0, foliageMaterial, batchArray, batchArray.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off);
        }
    }
}
