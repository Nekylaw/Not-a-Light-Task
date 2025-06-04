using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class FoliageChunkDebugger : MonoBehaviour
{
    public Vector2Int chunkSize = new Vector2Int(100, 100);
    public Color gridColor = new Color(0f, 1f, 0f, 0.2f);
    public int gridRadius = 5; // how many chunks in each direction

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;

        Vector3 center = transform.position;
        Vector3 origin = new Vector3(
            Mathf.Floor(center.x / chunkSize.x) * chunkSize.x,
            0,
            Mathf.Floor(center.z / chunkSize.y) * chunkSize.y
        );

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int z = -gridRadius; z <= gridRadius; z++)
            {
                Vector3 chunkPos = origin + new Vector3(x * chunkSize.x, 0, z * chunkSize.y);
                Gizmos.DrawWireCube(chunkPos + new Vector3(chunkSize.x, 0, chunkSize.y) * 0.5f, new Vector3(chunkSize.x, 0, chunkSize.y));
            }
        }
    }
}
