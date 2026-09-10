using System.Collections.Generic;
using UnityEngine;

public class PathPreview : MonoBehaviour
{
    public Pathfinding pathfinding;
    public ClickManager clickManager;

    [Header("Direction Sprites")]
    public Sprite arrowN;
    public Sprite arrowNE;
    public Sprite arrowE;
    public Sprite arrowSE;
    public Sprite arrowS;
    public Sprite arrowSW;
    public Sprite arrowW;
    public Sprite arrowNW;

    [SerializeField] private float heightAboveFloor = 0.12f;

    private List<GameObject> activePathTiles = new List<GameObject>();
    private Vector2Int lastCell;
    private bool hasLastCell = false;
    private bool warnedMissingMainCamera = false;

    void Update()
    {
        if (clickManager == null || clickManager.actionMode != ClickManager.ActionMode.Move)
        {
            ClearPath();
            hasLastCell = false;
            return;
        }

        PlayerMovement selectedUnit = clickManager.SelectedUnit;
        UnitStats stats = clickManager.SelectedUnitStats;

        if (selectedUnit == null || stats == null || stats.isDowned || pathfinding == null)
        {
            ClearPath();
            hasLastCell = false;
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            ClearPath();
            hasLastCell = false;

            if (!warnedMissingMainCamera)
            {
                Debug.LogWarning("PathPreview nao encontrou Camera.main.");
                warnedMissingMainCamera = true;
            }

            return;
        }

        warnedMissingMainCamera = false;
        GridManager grid = pathfinding.Grid;
        if (grid == null)
        {
            ClearPath();
            hasLastCell = false;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int currentCell = grid.WorldToCell(hit.point);
            if (!grid.IsCellValid(currentCell))
            {
                ClearPath();
                hasLastCell = false;
                return;
            }

            if (hasLastCell && currentCell == lastCell)
                return;

            lastCell = currentCell;
            hasLastCell = true;

            PathResult path = pathfinding.GetReachablePath(
                selectedUnit.transform.position,
                currentCell,
                stats
            );

            if (path == null || !path.HasSteps || path.totalCost > stats.currentMovePoints)
            {
                ClearPath();
                return;
            }

            ShowPath(path.cells, grid.WorldToCell(selectedUnit.transform.position), grid);
        }
        else
        {
            ClearPath();
            hasLastCell = false;
        }
    }

    private void ShowPath(List<Vector2Int> path, Vector2Int startCell, GridManager grid)
    {
        ClearPath();

        if (path == null)
            return;

        Vector2Int previousCell = startCell;
        foreach (Vector2Int cell in path)
        {
            if (!grid.IsCellValid(cell))
                continue;

            Sprite arrow = GetArrowSprite(cell - previousCell);
            previousCell = cell;

            if (arrow == null)
                continue;

            GameObject tile = new GameObject($"Path Arrow {cell.x},{cell.y}");
            tile.transform.position = grid.CellToWorld(cell, heightAboveFloor);
            tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = Vector3.one * grid.cellSize;

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = arrow;
            renderer.sortingOrder = 1;

            grid.ValidateCellVisual(tile, cell, "Preview");

            activePathTiles.Add(tile);
        }
    }

    private Sprite GetArrowSprite(Vector2Int direction)
    {
        direction = new Vector2Int(
            System.Math.Sign(direction.x),
            System.Math.Sign(direction.y)
        );

        if (direction == Vector2Int.up) return arrowN;
        if (direction == new Vector2Int(1, 1)) return arrowNE;
        if (direction == Vector2Int.right) return arrowE;
        if (direction == new Vector2Int(1, -1)) return arrowSE;
        if (direction == Vector2Int.down) return arrowS;
        if (direction == new Vector2Int(-1, -1)) return arrowSW;
        if (direction == Vector2Int.left) return arrowW;
        if (direction == new Vector2Int(-1, 1)) return arrowNW;

        return null;
    }

    public void ClearPath()
    {
        foreach (GameObject tile in activePathTiles)
        {
            if (tile != null)
                Destroy(tile);
        }

        activePathTiles.Clear();
    }
}
