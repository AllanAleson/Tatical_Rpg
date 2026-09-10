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
        GridManager grid = pathfinding.Grid;
        if (grid == null)
        {
            Debug.LogWarning("MovementHighlighter nao encontrou GridManager.");
            return;
        }

        Vector2Int unitCell = grid.WorldToCell(unitPosition);
        int movePoints = unitStats.currentMovePoints;
        Dictionary<Vector2Int, int> reachableCosts =
            pathfinding.GetReachableCosts(unitPosition, unitStats, movePoints);
        HashSet<Vector2Int> shownCells = new HashSet<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, int> reachable in reachableCosts)
        {
            SpawnTile(reachable.Key, moveTilePrefab, grid);
            shownCells.Add(reachable.Key);
        }

        for (int x = -movePoints; x <= movePoints; x++)
        {
            for (int z = -movePoints; z <= movePoints; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);

                if (distance <= 0 || distance > movePoints)
                    continue;

                Vector2Int cell = unitCell + new Vector2Int(x, z);

                if (!grid.IsCellValid(cell) || shownCells.Contains(cell))
                    continue;

                SpawnTile(cell, blockedTilePrefab, grid);
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

    private void SpawnTile(Vector2Int cell, GameObject prefab, GridManager grid)
    {
        if (prefab == null)
            return;

        if (grid == null || !grid.IsCellValid(cell))
            return;

        Vector3 tilePosition = grid.CellToWorld(cell, 0.04f);
        GameObject tile = Instantiate(prefab, tilePosition, Quaternion.identity);
        grid.ValidateCellVisual(tile, cell, "Highlight");

        activeTiles.Add(tile);
    }
}
