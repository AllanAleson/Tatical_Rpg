using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public bool allowDiagonal = false;

    public List<Vector2Int> FindPath(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)
    {
        if (moverStats == null)
        {
            Debug.LogWarning("Pathfinding.FindPath recebeu moverStats nulo.");
            return null;
        }

        if (moverStats.isDowned)
            return null;

        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(startPos.x),
            Mathf.RoundToInt(startPos.z)
        );

        Vector2Int target = new Vector2Int(targetX, targetZ);

        if (start == target)
            return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> distance = new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);
        cameFrom[start] = start;
        distance[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == target)
                break;

            foreach (Vector2Int dir in GetDirections())
            {
                Vector2Int next = current + dir;

                if (cameFrom.ContainsKey(next))
                    continue;

                if (IsBlocked(next, moverStats.gameObject))
                    continue;

                int newDistance = distance[current] + 1;

                if (newDistance > moverStats.currentMovePoints)
                    continue;

                queue.Enqueue(next);
                cameFrom[next] = current;
                distance[next] = newDistance;
            }
        }

        if (!cameFrom.ContainsKey(target))
            return null;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int pathCell = target;

        while (pathCell != start)
        {
            path.Add(pathCell);
            pathCell = cameFrom[pathCell];
        }

        path.Reverse();
        return path;
    }

    public bool CanReach(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)
    {
        return FindPath(startPos, targetX, targetZ, moverStats) != null;
    }

    public bool IsBlocked(Vector2Int cell, GameObject ignoredObject)
    {
        Vector3 checkPosition = new Vector3(cell.x, 0.5f, cell.y);

        Collider[] hits = Physics.OverlapBox(
            checkPosition,
            new Vector3(0.4f, 0.4f, 0.4f)
        );

        foreach (Collider hit in hits)
        {
            if (ignoredObject != null && (hit.gameObject == ignoredObject || hit.transform.IsChildOf(ignoredObject.transform)))
                continue;

            UnitStats stats = hit.GetComponentInParent<UnitStats>();

            if (stats != null)
            {
                if (stats.isDowned)
                    continue;

                return true;
            }

            if (hit.CompareTag("Obstacle"))
                return true;
        }

        return false;
    }

    public Vector2Int[] GetDirections()
    {
        if (allowDiagonal)
        {
            return new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(1, 1),
                new Vector2Int(1, -1),
                new Vector2Int(-1, 1),
                new Vector2Int(-1, -1)
            };
        }

        return new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
    }
}
