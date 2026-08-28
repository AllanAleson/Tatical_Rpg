using System.Collections.Generic;
using UnityEngine;

public class PathPreview : MonoBehaviour
{
    public GameObject pathTilePrefab;
    public Pathfinding pathfinding;
    public ClickManager clickManager;

    private List<GameObject> activePathTiles = new List<GameObject>();
    private Vector2Int lastCell;
    private bool hasLastCell = false;

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
            Debug.LogWarning("PathPreview nao encontrou Camera.main.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int gridX = Mathf.RoundToInt(hit.point.x);
            int gridZ = Mathf.RoundToInt(hit.point.z);
            Vector2Int currentCell = new Vector2Int(gridX, gridZ);

            if (hasLastCell && currentCell == lastCell)
                return;

            lastCell = currentCell;
            hasLastCell = true;

            List<Vector2Int> path = pathfinding.FindPath(
                selectedUnit.transform.position,
                gridX,
                gridZ,
                stats
            );

            if (path == null || path.Count <= 0 || path.Count > stats.currentMovePoints)
            {
                ClearPath();
                return;
            }

            ShowPath(path);
        }
        else
        {
            ClearPath();
            hasLastCell = false;
        }
    }

    private void ShowPath(List<Vector2Int> path)
    {
        ClearPath();

        if (path == null || pathTilePrefab == null)
            return;

        foreach (Vector2Int cell in path)
        {
            Vector3 position = new Vector3(cell.x, 0.12f, cell.y);
            GameObject tile = Instantiate(pathTilePrefab, position, Quaternion.identity);

            activePathTiles.Add(tile);
        }
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
