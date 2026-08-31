using System.Collections.Generic;
using UnityEngine;

public enum PathfindingSearchMode
{
    Auto,
    AStar,
    Dijkstra
}

public class PathResult
{
    public List<Vector2Int> cells = new List<Vector2Int>();
    public List<int> stepCosts = new List<int>();
    public int totalCost = 0;
    public bool success = false;
    public PathfindingSearchMode searchMode = PathfindingSearchMode.Auto;

    public bool HasSteps
    {
        get { return success && cells != null && cells.Count > 0; }
    }
}

public struct SpecialMovementConnection
{
    public Vector2Int origin;
    public Vector2Int destination;
    public int cost;
    public bool canBreakSpatialHeuristic;
}

public class Pathfinding : MonoBehaviour
{
    public bool allowDiagonal = false;
    public int maxSearchIterations = 10000;

    private int nextSpecialConnectionId = 1;
    private readonly Dictionary<int, SpecialMovementConnection> specialConnections =
        new Dictionary<int, SpecialMovementConnection>();
    private readonly Dictionary<Vector2Int, List<int>> specialConnectionsByOrigin =
        new Dictionary<Vector2Int, List<int>>();
    private int heuristicBreakingSpecialConnectionCount = 0;

    public List<Vector2Int> FindPath(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)
    {
        PathResult result = FindPathResult(startPos, targetX, targetZ, moverStats);

        if (result == null || !result.HasSteps)
            return null;

        return result.cells;
    }

    public PathResult FindPathResult(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)
    {
        return FindPathResult(
            startPos,
            new Vector2Int(targetX, targetZ),
            moverStats,
            PathfindingSearchMode.Auto,
            -1
        );
    }

    public PathResult FindPathResult(
        Vector3 startPos,
        Vector2Int target,
        UnitStats moverStats,
        PathfindingSearchMode searchMode = PathfindingSearchMode.Auto,
        int maxCost = -1)
    {
        if (moverStats == null)
        {
            Debug.LogWarning("Pathfinding.FindPath recebeu moverStats nulo.");
            return PathFailure();
        }

        if (moverStats.isDowned)
            return PathFailure();

        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(startPos.x),
            Mathf.RoundToInt(startPos.z)
        );

        if (start == target)
            return PathFailure();

        PathfindingSearchMode resolvedMode = ResolveSearchMode(searchMode);

        Debug.Log(
            $"[Pathfinding] Iniciando busca {resolvedMode} para {moverStats.name} de {start} ate {target}. PM disponiveis: {moverStats.currentMovePoints}"
        );

        List<Vector2Int> openSet = new List<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>();
        Dictionary<Vector2Int, int> stepCostFromPrevious = new Dictionary<Vector2Int, int>();

        openSet.Add(start);
        cameFrom[start] = start;
        costSoFar[start] = 0;

        int iterations = 0;

        while (openSet.Count > 0)
        {
            if (maxSearchIterations > 0 && iterations >= maxSearchIterations)
            {
                Debug.LogWarning(
                    $"[Pathfinding] Busca interrompida por limite de iteracoes ({maxSearchIterations})."
                );

                break;
            }

            iterations++;

            Vector2Int current = PopLowestPriority(openSet, costSoFar, target, resolvedMode);

            if (current == target)
                break;

            foreach (MovementEdge edge in GetMovementEdges(current, moverStats))
            {
                Vector2Int next = edge.destination;

                if (IsBlocked(next, moverStats.gameObject))
                    continue;

                if (edge.cost < 0)
                    continue;

                int newCost = costSoFar[current] + edge.cost;

                if (maxCost >= 0 && newCost > maxCost)
                    continue;

                if (costSoFar.ContainsKey(next) && newCost >= costSoFar[next])
                    continue;

                if (!openSet.Contains(next))
                    openSet.Add(next);

                cameFrom[next] = current;
                costSoFar[next] = newCost;
                stepCostFromPrevious[next] = edge.cost;
            }
        }

        if (!cameFrom.ContainsKey(target))
        {
            Debug.Log($"[Pathfinding] FALHA: caminho nao encontrado para '{moverStats.name}'.");
            return PathFailure(resolvedMode);
        }

        PathResult result = BuildPathResult(
            start,
            target,
            cameFrom,
            stepCostFromPrevious,
            costSoFar[target],
            resolvedMode
        );

        Debug.Log(
            $"[Pathfinding] SUCESSO: caminho encontrado para '{moverStats.name}' com {result.cells.Count} celulas e custo {result.totalCost} PM."
        );

        return result;
    }

    public bool CanReach(Vector3 startPos, int targetX, int targetZ, UnitStats moverStats)
    {
        if (moverStats == null)
            return false;

        PathResult result = FindPathResult(
            startPos,
            new Vector2Int(targetX, targetZ),
            moverStats,
            PathfindingSearchMode.Auto,
            moverStats.currentMovePoints
        );

        return result != null && result.success && result.totalCost <= moverStats.currentMovePoints;
    }

    public PathResult GetReachablePath(Vector3 startPos, Vector2Int target, UnitStats moverStats)
    {
        if (moverStats == null)
            return PathFailure();

        return FindPathResult(
            startPos,
            target,
            moverStats,
            PathfindingSearchMode.Auto,
            moverStats.currentMovePoints
        );
    }

    public Dictionary<Vector2Int, int> GetReachableCosts(Vector3 startPos, UnitStats moverStats, int maxCost)
    {
        Dictionary<Vector2Int, int> reachableCosts = new Dictionary<Vector2Int, int>();

        if (moverStats == null || moverStats.isDowned || maxCost <= 0)
            return reachableCosts;

        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(startPos.x),
            Mathf.RoundToInt(startPos.z)
        );

        List<Vector2Int> openSet = new List<Vector2Int> { start };
        Dictionary<Vector2Int, int> costSoFar = new Dictionary<Vector2Int, int>
        {
            [start] = 0
        };

        int iterations = 0;

        while (openSet.Count > 0)
        {
            if (maxSearchIterations > 0 && iterations >= maxSearchIterations)
            {
                Debug.LogWarning(
                    $"[Pathfinding] Alcance interrompido por limite de iteracoes ({maxSearchIterations})."
                );

                break;
            }

            iterations++;

            Vector2Int current = PopLowestPriority(
                openSet,
                costSoFar,
                start,
                PathfindingSearchMode.Dijkstra
            );

            foreach (MovementEdge edge in GetMovementEdges(current, moverStats))
            {
                Vector2Int next = edge.destination;

                if (next != start && IsBlocked(next, moverStats.gameObject))
                    continue;

                int newCost = costSoFar[current] + edge.cost;

                if (newCost > maxCost)
                    continue;

                if (costSoFar.ContainsKey(next) && newCost >= costSoFar[next])
                    continue;

                costSoFar[next] = newCost;

                if (!openSet.Contains(next))
                    openSet.Add(next);
            }
        }

        foreach (KeyValuePair<Vector2Int, int> pair in costSoFar)
        {
            if (pair.Key != start)
                reachableCosts[pair.Key] = pair.Value;
        }

        return reachableCosts;
    }

    public PathResult TrimPathToMovePoints(PathResult path, int availableMovePoints)
    {
        PathResult trimmed = new PathResult
        {
            success = false,
            searchMode = path != null ? path.searchMode : PathfindingSearchMode.Auto
        };

        if (path == null || !path.success || path.cells == null || path.stepCosts == null)
            return trimmed;

        int runningCost = 0;

        for (int i = 0; i < path.cells.Count; i++)
        {
            int stepCost = i < path.stepCosts.Count ? path.stepCosts[i] : 0;

            if (runningCost + stepCost > availableMovePoints)
                break;

            runningCost += stepCost;
            trimmed.cells.Add(path.cells[i]);
            trimmed.stepCosts.Add(stepCost);
        }

        trimmed.totalCost = runningCost;
        trimmed.success = trimmed.cells.Count > 0;

        return trimmed;
    }

    public int RegisterSpecialConnection(Vector2Int origin, Vector2Int destination, int cost)
    {
        if (cost < 0)
        {
            Debug.LogWarning("Pathfinding nao aceita conexoes especiais com custo negativo.");
            return 0;
        }

        SpecialMovementConnection connection = new SpecialMovementConnection
        {
            origin = origin,
            destination = destination,
            cost = cost,
            canBreakSpatialHeuristic = CanConnectionBreakSpatialHeuristic(origin, destination, cost)
        };

        int id = nextSpecialConnectionId++;
        specialConnections[id] = connection;

        if (!specialConnectionsByOrigin.TryGetValue(origin, out List<int> ids))
        {
            ids = new List<int>();
            specialConnectionsByOrigin[origin] = ids;
        }

        ids.Add(id);

        if (connection.canBreakSpatialHeuristic)
            heuristicBreakingSpecialConnectionCount++;

        return id;
    }

    public bool UnregisterSpecialConnection(int connectionId)
    {
        if (!specialConnections.TryGetValue(connectionId, out SpecialMovementConnection connection))
            return false;

        specialConnections.Remove(connectionId);

        if (specialConnectionsByOrigin.TryGetValue(connection.origin, out List<int> ids))
        {
            ids.Remove(connectionId);

            if (ids.Count == 0)
                specialConnectionsByOrigin.Remove(connection.origin);
        }

        if (connection.canBreakSpatialHeuristic)
            heuristicBreakingSpecialConnectionCount = Mathf.Max(0, heuristicBreakingSpecialConnectionCount - 1);

        return true;
    }

    public bool HasHeuristicBreakingSpecialConnections()
    {
        return heuristicBreakingSpecialConnectionCount > 0;
    }

    public int GetMovementCost(Vector2Int current, Vector2Int next, UnitStats moverStats)
    {
        int deltaX = Mathf.Abs(next.x - current.x);
        int deltaY = Mathf.Abs(next.y - current.y);

        if (deltaX == 1 && deltaY == 1)
            return 2;

        if (deltaX + deltaY == 1)
            return 1;

        return int.MaxValue;
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

    private PathfindingSearchMode ResolveSearchMode(PathfindingSearchMode requestedMode)
    {
        if (requestedMode == PathfindingSearchMode.Auto)
        {
            if (HasHeuristicBreakingSpecialConnections())
                return PathfindingSearchMode.Dijkstra;

            return PathfindingSearchMode.AStar;
        }

        return requestedMode;
    }

    private Vector2Int PopLowestPriority(
        List<Vector2Int> openSet,
        Dictionary<Vector2Int, int> costSoFar,
        Vector2Int target,
        PathfindingSearchMode searchMode)
    {
        int bestIndex = 0;
        int bestPriority = GetPriority(openSet[0], costSoFar, target, searchMode);

        for (int i = 1; i < openSet.Count; i++)
        {
            int priority = GetPriority(openSet[i], costSoFar, target, searchMode);

            if (priority < bestPriority)
            {
                bestPriority = priority;
                bestIndex = i;
            }
        }

        Vector2Int current = openSet[bestIndex];
        openSet.RemoveAt(bestIndex);
        return current;
    }

    private int GetPriority(
        Vector2Int cell,
        Dictionary<Vector2Int, int> costSoFar,
        Vector2Int target,
        PathfindingSearchMode searchMode)
    {
        int heuristic = searchMode == PathfindingSearchMode.AStar
            ? GetHeuristicCost(cell, target)
            : 0;

        return costSoFar[cell] + heuristic;
    }

    private int GetHeuristicCost(Vector2Int from, Vector2Int to)
    {
        int deltaX = Mathf.Abs(to.x - from.x);
        int deltaY = Mathf.Abs(to.y - from.y);

        if (allowDiagonal)
        {
            int diagonalSteps = Mathf.Min(deltaX, deltaY);
            int orthogonalSteps = Mathf.Abs(deltaX - deltaY);
            return diagonalSteps * 2 + orthogonalSteps;
        }

        return deltaX + deltaY;
    }

    private IEnumerable<MovementEdge> GetMovementEdges(Vector2Int current, UnitStats moverStats)
    {
        foreach (Vector2Int direction in GetDirections())
        {
            Vector2Int next = current + direction;

            if (!CanMoveInDirection(current, direction, moverStats))
                continue;

            int cost = GetMovementCost(current, next, moverStats);

            if (cost != int.MaxValue)
                yield return new MovementEdge(next, cost);
        }

        if (!specialConnectionsByOrigin.TryGetValue(current, out List<int> ids))
            yield break;

        foreach (int id in ids)
        {
            if (!specialConnections.TryGetValue(id, out SpecialMovementConnection connection))
                continue;

            yield return new MovementEdge(connection.destination, connection.cost);
        }
    }

    private bool CanMoveInDirection(Vector2Int current, Vector2Int direction, UnitStats moverStats)
    {
        if (Mathf.Abs(direction.x) != 1 || Mathf.Abs(direction.y) != 1)
            return true;

        GameObject ignoredObject = moverStats != null ? moverStats.gameObject : null;
        Vector2Int horizontalCell = current + new Vector2Int(direction.x, 0);
        Vector2Int verticalCell = current + new Vector2Int(0, direction.y);

        return
            !IsBlocked(horizontalCell, ignoredObject) &&
            !IsBlocked(verticalCell, ignoredObject);
    }

    private bool CanConnectionBreakSpatialHeuristic(Vector2Int origin, Vector2Int destination, int cost)
    {
        int spatialCost = GetHeuristicCost(origin, destination);

        if (spatialCost > 2)
            return true;

        return cost < spatialCost;
    }

    private PathResult BuildPathResult(
        Vector2Int start,
        Vector2Int target,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Dictionary<Vector2Int, int> stepCostFromPrevious,
        int totalCost,
        PathfindingSearchMode searchMode)
    {
        PathResult result = new PathResult
        {
            success = true,
            totalCost = totalCost,
            searchMode = searchMode
        };

        Vector2Int pathCell = target;

        while (pathCell != start)
        {
            result.cells.Add(pathCell);
            result.stepCosts.Add(stepCostFromPrevious[pathCell]);
            pathCell = cameFrom[pathCell];
        }

        result.cells.Reverse();
        result.stepCosts.Reverse();

        return result;
    }

    private PathResult PathFailure(PathfindingSearchMode searchMode = PathfindingSearchMode.Auto)
    {
        return new PathResult
        {
            success = false,
            totalCost = 0,
            searchMode = searchMode
        };
    }

    private struct MovementEdge
    {
        public Vector2Int destination;
        public int cost;

        public MovementEdge(Vector2Int destination, int cost)
        {
            this.destination = destination;
            this.cost = cost;
        }
    }
}
