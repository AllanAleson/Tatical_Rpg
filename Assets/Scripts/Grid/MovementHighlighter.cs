using System.Collections.Generic;
using UnityEngine;

public class MovementHighlighter : MonoBehaviour
{
    public GameObject moveTilePrefab;
    public GameObject blockedTilePrefab;
    public Pathfinding pathfinding;

    private List<GameObject> activeTiles = new List<GameObject>();

    public void ShowMoveRange(PlayerMovement unit, UnitStats unitStats)
    {
        ClearTiles();

        if (unit == null || unitStats == null)
        {
            Debug.LogWarning("MovementHighlighter recebeu unidade ou UnitStats nulo.");
            return;
        }

        if (pathfinding == null)
        {
            Debug.LogWarning("MovementHighlighter esta sem referencia de Pathfinding.");
            return;
        }

        if (unitStats.isDowned || unitStats.currentMovePoints <= 0)
            return;

        Vector3 unitPosition = unit.transform.position;
        int unitX = Mathf.RoundToInt(unitPosition.x);
        int unitZ = Mathf.RoundToInt(unitPosition.z);
        int movePoints = unitStats.currentMovePoints;
        Dictionary<Vector2Int, int> reachableCosts =
            pathfinding.GetReachableCosts(unitPosition, unitStats, movePoints);
        HashSet<Vector2Int> shownCells = new HashSet<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, int> reachable in reachableCosts)
        {
            SpawnTile(reachable.Key, moveTilePrefab);
            shownCells.Add(reachable.Key);
        }

        for (int x = -movePoints; x <= movePoints; x++)
        {
            for (int z = -movePoints; z <= movePoints; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);

                if (distance <= 0 || distance > movePoints)
                    continue;

                int cellX = unitX + x;
                int cellZ = unitZ + z;
                Vector2Int cell = new Vector2Int(cellX, cellZ);

                if (shownCells.Contains(cell))
                    continue;

                SpawnTile(cell, blockedTilePrefab);
                shownCells.Add(cell);
            }
        }
    }

    public void ClearTiles()
    {
        foreach (GameObject tile in activeTiles)
        {
            if (tile != null)
                Destroy(tile);
        }

        activeTiles.Clear();
    }

    private void SpawnTile(Vector2Int cell, GameObject prefab)
    {
        if (prefab == null)
            return;

        Vector3 tilePosition = new Vector3(cell.x, 0.04f, cell.y);
        GameObject tile = Instantiate(prefab, tilePosition, Quaternion.identity);

        activeTiles.Add(tile);
    }
}
