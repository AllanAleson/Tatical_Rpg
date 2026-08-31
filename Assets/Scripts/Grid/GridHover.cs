using System.Collections.Generic;
using UnityEngine;

public class GridHover : MonoBehaviour
{
    public GameObject hoverTargetPrefab;
    public MoveCostUI moveCostUI;
    public Pathfinding pathfinding;
    public ClickManager clickManager;

    private GameObject hoverTarget;

    void Start()
    {
        if (hoverTargetPrefab == null)
        {
            Debug.LogWarning("GridHover esta sem hoverTargetPrefab.");
            return;
        }

        hoverTarget = Instantiate(hoverTargetPrefab);
        hoverTarget.SetActive(false);
    }

    void Update()
    {
        if (clickManager == null)
        {
            HideHover();
            return;
        }

        if (clickManager.actionMode != ClickManager.ActionMode.Move)
        {
            HideHover();
            return;
        }

        PlayerMovement selectedUnit = clickManager.SelectedUnit;
        UnitStats stats = clickManager.SelectedUnitStats;

        if (selectedUnit == null || stats == null || stats.isDowned || pathfinding == null)
        {
            HideHover();
            return;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            HideHover();
            Debug.LogWarning("GridHover nao encontrou Camera.main.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int gridX = Mathf.RoundToInt(hit.point.x);
            int gridZ = Mathf.RoundToInt(hit.point.z);

            Vector2Int targetCell = new Vector2Int(gridX, gridZ);
            PathResult path = pathfinding.GetReachablePath(
                selectedUnit.transform.position,
                targetCell,
                stats
            );

            if (path == null || !path.HasSteps || path.totalCost > stats.currentMovePoints)
            {
                HideHover();
                return;
            }

            int cost = path.totalCost;
            int remaining = stats.currentMovePoints - cost;
            Vector3 targetPosition = new Vector3(gridX, 0.15f, gridZ);

            if (moveCostUI != null)
                moveCostUI.ShowCost(cost, remaining, targetPosition);

            if (hoverTarget != null)
            {
                hoverTarget.transform.position = new Vector3(gridX, 0.08f, gridZ);
                hoverTarget.SetActive(true);
            }
        }
        else
        {
            HideHover();
        }
    }

    private void HideHover()
    {
        if (hoverTarget != null)
            hoverTarget.SetActive(false);

        if (moveCostUI != null)
            moveCostUI.HideCost();
    }
}
