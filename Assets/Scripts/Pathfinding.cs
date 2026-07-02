using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public UnitStats unitStats;
    public bool allowDiagonal = false;

    public List<Vector2Int> FindPath(Vector3 startPos, int targetX, int targetZ)
    {
        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(startPos.x),
            Mathf.RoundToInt(startPos.z)
        );

        Vector2Int target = new Vector2Int(targetX, targetZ);

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

if (IsBlocked(next, unitStats.gameObject))
    continue;

                int newDistance = distance[current] + 1;

                if (newDistance > unitStats.currentMovePoints)
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

    public bool CanReach(Vector3 startPos, int targetX, int targetZ)
    {
        return FindPath(startPos, targetX, targetZ) != null;
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
        // Ignora a própria unidade
        if (hit.gameObject == ignoredObject)
            continue;

        // Qualquer objeto que tenha UnitStats é uma unidade
        UnitStats stats = hit.GetComponent<UnitStats>();

        if (stats != null)
        {
            // Unidade desmaiada não bloqueia
            if (stats.isDowned)
                continue;

            // Unidade viva bloqueia
            return true;
        }

        // Qualquer obstáculo bloqueia
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