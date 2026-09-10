using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    public float speed = 4f;

    [Header("Debug")]
    public bool logDetailedMovement = false;

    private TurnManager.ActionExecution movementAction;
    private Coroutine movementRoutine;
    private Vector3 lastCompletedPosition;

    public void MoveAlongPath(PathResult path)
    {
        MoveAlongPath(path, null);
    }

    public void MoveAlongPath(PathResult path, TurnManager.ActionExecution parentAction)
    {
        UnitStats stats = GetComponent<UnitStats>();
        GridManager grid = GridManager.Instance != null
            ? GridManager.Instance
            : FindAnyObjectByType<GridManager>();
        if (!isActiveAndEnabled || IsMoving() || stats == null || speed <= 0f ||
            grid == null ||
            path == null || !path.HasSteps || path.stepCosts == null ||
            path.stepCosts.Count != path.cells.Count)
            return;

        long totalCost = 0;
        for (int i = 0; i < path.cells.Count; i++)
        {
            if (!grid.IsCellValid(path.cells[i]) ||
                grid.IsCellBlocked(path.cells[i], gameObject))
                return;

            int cost = path.stepCosts[i];
            if (cost < 0)
                return;
            totalCost += cost;
        }

        if (totalCost != path.totalCost || totalCost > stats.currentMovePoints)
            return;

        TurnManager turns = parentAction != null ? parentAction.Manager :
            FindAnyObjectByType<TurnManager>();
        if (turns == null || !turns.TryBeginAction(stats, out var action, parentAction))
            return;

        movementAction = action;
        lastCompletedPosition = transform.position;
        // Own a snapshot so callers cannot mutate a route already executing.
        movementRoutine = StartCoroutine(MoveRoutine(new List<Vector2Int>(path.cells),
            new List<int>(path.stepCosts), stats, grid, action));
    }

    public void MoveAlongPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
            return;

        PathResult result = new PathResult { success = true, cells = new List<Vector2Int>(path) };
        GridManager grid = GridManager.Instance != null
            ? GridManager.Instance
            : FindAnyObjectByType<GridManager>();
        if (grid == null)
            return;

        Vector2Int previous = grid.WorldToCell(transform.position);

        foreach (Vector2Int cell in path)
        {
            int x = Mathf.Abs(cell.x - previous.x);
            int z = Mathf.Abs(cell.y - previous.y);
            if (x > 1 || z > 1 || x + z == 0)
                return; // Non-adjacent connections require explicit PathResult costs.
            int cost = x + z;
            result.stepCosts.Add(cost);
            result.totalCost += cost;
            previous = cell;
        }
        MoveAlongPath(result);
    }

    private IEnumerator MoveRoutine(List<Vector2Int> path, List<int> stepCosts,
        UnitStats stats, GridManager grid, TurnManager.ActionExecution action)
    {
        Vector2Int origin = grid.WorldToCell(transform.position);
        int spent = 0;
        try
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (!action.IsAuthorized || !isActiveAndEnabled || speed <= 0f ||
                    !grid.IsCellValid(path[i]) ||
                    grid.IsCellBlocked(path[i], gameObject) ||
                    stats.currentMovePoints < stepCosts[i])
                    yield break;

                Vector3 targetPosition = grid.CellToWorld(path[i], 0.5f);
                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    // Authorization is checked every frame, including between steps.
                    if (!action.IsAuthorized || !isActiveAndEnabled || speed <= 0f ||
                        !grid.IsCellValid(path[i]) ||
                        grid.IsCellBlocked(path[i], gameObject) ||
                        stats.currentMovePoints < stepCosts[i])
                        yield break;

                    transform.position = Vector3.MoveTowards(transform.position,
                        targetPosition, speed * Time.deltaTime);
                    yield return null;
                }

                if (!action.IsAuthorized || !grid.IsCellValid(path[i]) ||
                    grid.IsCellBlocked(path[i], gameObject) ||
                    stats.currentMovePoints < stepCosts[i])
                    yield break;

                transform.position = targetPosition;
                lastCompletedPosition = targetPosition;
                // Commit each completed step exactly once. Incomplete steps roll back
                // in cleanup and cost nothing; there is no total charge at the end.
                stats.SpendMovePoints(stepCosts[i]);
                spent += stepCosts[i];
            }

            Debug.Log($"Movimento: {stats.name} {origin} -> {path[path.Count - 1]}; gasto {spent} PM; restante {stats.currentMovePoints} PM.");
            if (logDetailedMovement)
                Debug.Log(BuildDetailedMovementLog(stats, origin, path, stepCosts));
        }
        finally
        {
            FinishMovement(action);
        }
    }

    public bool IsMoving()
    {
        return movementAction != null;
    }

    public void CancelMovement()
    {
        var action = movementAction;
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }
        FinishMovement(action); // Also covers Unity stopping a coroutine on disable.
    }

    void OnDisable()
    {
        CancelMovement();
    }

    private void FinishMovement(TurnManager.ActionExecution action)
    {
        if (action == null)
            return;
        if (movementAction == action)
        {
            transform.position = lastCompletedPosition;
            movementAction = null;
            movementRoutine = null;
        }
        action.Dispose();
    }

    private string BuildDetailedMovementLog(
        UnitStats stats,
        Vector2Int origin,
        List<Vector2Int> path,
        List<int> stepCosts)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        Vector2Int previous = origin;

        builder.Append("Movimento detalhado: ");
        builder.Append(stats.gameObject.name);
        builder.Append(" | ");
        builder.Append(origin);

        for (int i = 0; i < path.Count; i++)
        {
            int stepCost = i < stepCosts.Count ? stepCosts[i] : 0;

            builder.Append(" -> ");
            builder.Append(path[i]);
            builder.Append(" (");
            builder.Append(previous);
            builder.Append(">");
            builder.Append(path[i]);
            builder.Append(": ");
            builder.Append(stepCost);
            builder.Append(" PM)");

            previous = path[i];
        }

        return builder.ToString();
    }
}
