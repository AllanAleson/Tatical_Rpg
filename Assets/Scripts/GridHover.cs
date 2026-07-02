using System.Collections.Generic;
using UnityEngine;

public class GridHover : MonoBehaviour
{
    public GameObject hoverTargetPrefab;
    public PlayerMovement player;
    public MoveCostUI moveCostUI;
    public Pathfinding pathfinding;
    public ClickManager clickManager;

    private GameObject hoverTarget;

    void Start()
    {
        hoverTarget = Instantiate(hoverTargetPrefab);
        hoverTarget.SetActive(false);
    }

    void Update()
    {
        if (clickManager.actionMode != ClickManager.ActionMode.Move)
        {
            hoverTarget.SetActive(false);
            moveCostUI.HideCost();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int gridX = Mathf.RoundToInt(hit.point.x);
            int gridZ = Mathf.RoundToInt(hit.point.z);

            List<Vector2Int> path = pathfinding.FindPath(
                player.transform.position,
                gridX,
                gridZ
            );

            if (path == null)
            {
                hoverTarget.SetActive(false);
                moveCostUI.HideCost();
                return;
            }

            int cost = path.Count;

            UnitStats stats = player.GetComponent<UnitStats>();
            int remaining = stats.currentMovePoints - cost;

            Vector3 targetPosition = new Vector3(gridX, 0.15f, gridZ);

            moveCostUI.ShowCost(
                cost,
                remaining,
                targetPosition
            );

            hoverTarget.transform.position = new Vector3(gridX, 0.08f, gridZ);
            hoverTarget.SetActive(true);
        }
        else
        {
            hoverTarget.SetActive(false);
            moveCostUI.HideCost();
        }
    }
}