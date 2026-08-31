using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    public float speed = 4f;

    private bool isMoving = false;

    public void MoveAlongPath(PathResult path)
    {
        UnitStats stats = GetComponent<UnitStats>();

        if (stats == null)
        {
            Debug.LogWarning("UnitMovement sem UnitStats: " + gameObject.name);
            return;
        }

        if (stats.isDowned)
            return;

        if (!isMoving && path != null && path.HasSteps)
            StartCoroutine(MoveRoutine(path.cells, path.totalCost));
    }

    public void MoveAlongPath(List<Vector2Int> path)
    {
        UnitStats stats = GetComponent<UnitStats>();

        if (stats == null)
        {
            Debug.LogWarning("UnitMovement sem UnitStats: " + gameObject.name);
            return;
        }

        if (stats.isDowned)
            return;

        if (!isMoving && path != null && path.Count > 0)
            StartCoroutine(MoveRoutine(path, CalculateAdjacentPathCost(path)));
    }

    private IEnumerator MoveRoutine(List<Vector2Int> path, int movePointCost)
    {
        isMoving = true;

        foreach (Vector2Int cell in path)
        {
            Vector3 targetPosition = new Vector3(cell.x, 0.5f, cell.y);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;
        }

        UnitStats stats = GetComponent<UnitStats>();

        if (stats != null)
            stats.SpendMovePoints(movePointCost);

        isMoving = false;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    private int CalculateAdjacentPathCost(List<Vector2Int> path)
    {
        Vector2Int previous = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        int totalCost = 0;

        foreach (Vector2Int cell in path)
        {
            int deltaX = Mathf.Abs(cell.x - previous.x);
            int deltaY = Mathf.Abs(cell.y - previous.y);

            if (deltaX == 1 && deltaY == 1)
                totalCost += 2;
            else if (deltaX + deltaY == 1)
                totalCost += 1;
            else
                totalCost += 0;

            previous = cell;
        }

        return totalCost;
    }
}
