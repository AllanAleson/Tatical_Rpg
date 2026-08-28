using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        float startX = -(width * cellSize) / 2f;
        float startZ = -(height * cellSize) / 2f;

        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                new Vector3(startX + x * cellSize, 0.05f, startZ),
                new Vector3(startX + x * cellSize, 0.05f, startZ + height * cellSize)
            );
        }

        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(
                new Vector3(startX, 0.05f, startZ + z * cellSize),
                new Vector3(startX + width * cellSize, 0.05f, startZ + z * cellSize)
            );
        }
    }
}