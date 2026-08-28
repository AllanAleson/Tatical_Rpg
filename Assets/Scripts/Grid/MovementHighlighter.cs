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

                bool canReach = pathfinding.CanReach(unitPosition, cellX, cellZ, unitStats);

                if (pathfinding.IsBlocked(cell, unitStats.gameObject))
                    canReach = false;

                GameObject prefabToUse = canReach ? moveTilePrefab : blockedTilePrefab;

                if (prefabToUse == null)
                    continue;

                Vector3 tilePosition = new Vector3(cellX, 0.04f, cellZ);
                GameObject tile = Instantiate(prefabToUse, tilePosition, Quaternion.identity);

                activeTiles.Add(tile);
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
}
