using System.Collections.Generic;
using UnityEngine;

public class MovementHighlighter : MonoBehaviour
{
    public GameObject moveTilePrefab;
    public GameObject blockedTilePrefab;

    public UnitStats unitStats;
    public Pathfinding pathfinding;

    private List<GameObject> activeTiles = new List<GameObject>();

    public void ShowMoveRange(Vector3 playerPosition)
    {
        ClearTiles();

        int playerX = Mathf.RoundToInt(playerPosition.x);
        int playerZ = Mathf.RoundToInt(playerPosition.z);

        int movePoints = unitStats.currentMovePoints;

        for (int x = -movePoints; x <= movePoints; x++)
        {
            for (int z = -movePoints; z <= movePoints; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);

                if (distance <= movePoints)
                {
                    int cellX = playerX + x;
                    int cellZ = playerZ + z;

                    if (cellX == playerX && cellZ == playerZ)
                        continue;

                    bool canReach = pathfinding.CanReach(
                        playerPosition,
                        cellX,
                        cellZ
                    );

                    if (pathfinding.IsBlocked(
                        new Vector2Int(cellX, cellZ),
                        unitStats.gameObject))
                    {
                        canReach = false;
                    }

                    GameObject prefabToUse =
                        canReach ? moveTilePrefab : blockedTilePrefab;

                    Vector3 tilePosition = new Vector3(
                        cellX,
                        0.04f,
                        cellZ
                    );

                    GameObject tile = Instantiate(
                        prefabToUse,
                        tilePosition,
                        Quaternion.identity
                    );

                    activeTiles.Add(tile);
                }
            }
        }
    }

    public void ClearTiles()
    {
        foreach (GameObject tile in activeTiles)
        {
            Destroy(tile);
        }

        activeTiles.Clear();
    }
}