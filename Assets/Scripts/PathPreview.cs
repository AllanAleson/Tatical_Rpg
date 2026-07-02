using System.Collections.Generic;
using UnityEngine;

public class PathPreview : MonoBehaviour
{
    public GameObject pathTilePrefab;
    public PlayerMovement player;
    public Pathfinding pathfinding;
    public ClickManager clickManager;

    private List<GameObject> activePathTiles = new List<GameObject>();
    private Vector2Int lastCell;
    private bool hasLastCell = false;

    void Update()
    {
        if (clickManager.actionMode != ClickManager.ActionMode.Move)
        {
            ClearPath();
            hasLastCell = false;
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

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
                player.transform.position,
                gridX,
                gridZ
            );

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

        if (path == null)
            return;

        foreach (Vector2Int cell in path)
        {
            Vector3 position = new Vector3(cell.x, 0.12f, cell.y);

            GameObject tile = Instantiate(
                pathTilePrefab,
                position,
                Quaternion.identity
            );

            activePathTiles.Add(tile);
        }
    }

    public void ClearPath()
    {
        foreach (GameObject tile in activePathTiles)
        {
            Destroy(tile);
        }

        activePathTiles.Clear();
    }
}