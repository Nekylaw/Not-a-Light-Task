using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class FoliageChunkDebugger : MonoBehaviour
{

    public float fieldSize = 1500f;
    public Vector2Int chunkSize = new Vector2Int(100, 100);
    public Color color = new Color(0, 1, 0, 0.4f);



    private void OnDrawGizmos()
    {
        Vector2Int gridSize = new Vector2Int((int)fieldSize / chunkSize.x, (int)fieldSize / chunkSize.y);
        Gizmos.color = color;

        Vector3 gridOffset = new Vector3(
                -gridSize.x * chunkSize.x / 2f + chunkSize.x / 2f,
                0,
                -gridSize.y * chunkSize.y / 2f + chunkSize.y / 2f
            );

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 center = new Vector3(
                    x * chunkSize.x,
                    0,
                    y * chunkSize.y
                ) + gridOffset;

                Vector3 size = new Vector3(chunkSize.x, 0.1f, chunkSize.y);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
