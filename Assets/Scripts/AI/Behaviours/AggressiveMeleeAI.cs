using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tactical RPG/AI/Aggressive Melee")]
public class AggressiveMeleeAI : EnemyAIBehaviour
{
    public override IEnumerator ExecuteTurn(EnemyAITurnContext context)
    {
        if (!IsValidContext(context))
        {
            LogInvalidContext(context);
            yield break;
        }

        if (context.decisionDelay > 0f)
            yield return new WaitForSeconds(context.decisionDelay);

        UnitStats target = FindBestLivingPlayer(context);

        if (target == null)
        {
            Debug.Log("[AI] " + context.unitStats.gameObject.name + " nao encontrou alvo vivo.");
            Debug.Log("[AI] " + context.unitStats.gameObject.name + " nao encontrou acao valida.");
            yield break;
        }

        Debug.Log(
            "[AI] " + context.unitStats.gameObject.name +
            " escolheu " + target.gameObject.name + " como alvo."
        );

        // ---------------------------------------------------------
        // 1. Se ja estiver em alcance, usa todos os PA possiveis.
        // ---------------------------------------------------------
        bool attacked = false;

        while (
            target != null &&
            !target.isDowned &&
            context.unitStats.currentActionPoints >= context.unitStats.GetCurrentAttackCost())
        {
            if (!CombatActions.TryBasicAttack(
                    context.unitStats,
                    target,
                    out string failureReason))
            {
                break;
            }

            attacked = true;

            yield return FinishAttack(context, target);

            Debug.Log(
                "[AI] " + context.unitStats.gameObject.name +
                " possui " + context.unitStats.currentActionPoints +
                " PA restante."
            );
        }

        // Se conseguiu atacar antes de mover, o alvo estava em alcance.
        // Caso ele tenha derrubado o alvo e ainda tenha PA, tenta outro.
        if (attacked)
        {
            while (context.unitStats.currentActionPoints >= context.unitStats.GetCurrentAttackCost())
            {
                target = FindBestLivingPlayer(context);

                if (target == null)
                    break;

                if (!CombatActions.TryBasicAttack(
                        context.unitStats,
                        target,
                        out string failureReason))
                {
                    break;
                }

                yield return FinishAttack(context, target);
            }

            yield break;
        }

        // ---------------------------------------------------------
        // 2. Nao consegue atacar -> tenta se aproximar.
        // ---------------------------------------------------------
        PathResult path = FindBestMovementPath(context, target);

        if (path != null && path.HasSteps)
        {
            int spentMovePoints = path.totalCost;

            context.movement.MoveAlongPath(path);

            while (context.movement.IsMoving())
                yield return null;

            Debug.Log(
                "[AI] " + context.unitStats.gameObject.name +
                " moveu " + path.cells.Count +
                " celulas / gastou " + spentMovePoints + " PM."
            );

            if (context.afterMoveDelay > 0f)
                yield return new WaitForSeconds(context.afterMoveDelay);
        }
        else
        {
            Debug.Log(
                "[AI] " + context.unitStats.gameObject.name +
                " nao encontrou rota valida."
            );
        }

        // ---------------------------------------------------------
        // 3. Depois de mover, usa TODOS os PA possiveis.
        // ---------------------------------------------------------
        while (
            target != null &&
            !target.isDowned &&
            context.unitStats.currentActionPoints >= context.unitStats.GetCurrentAttackCost())
        {
            if (!CombatActions.TryBasicAttack(
                    context.unitStats,
                    target,
                    out string failureReason))
            {
                break;
            }

            yield return FinishAttack(context, target);

            Debug.Log(
                "[AI] " + context.unitStats.gameObject.name +
                " possui " + context.unitStats.currentActionPoints +
                " PA restante."
            );
        }

        // ---------------------------------------------------------
        // 4. Se derrubou o alvo mas ainda tem PA,
        // tenta atacar outro alvo que esteja em alcance.
        // ---------------------------------------------------------
        while (context.unitStats.currentActionPoints >= context.unitStats.GetCurrentAttackCost())
        {
            UnitStats newTarget = FindBestLivingPlayer(context);

            if (newTarget == null)
                break;

            // Se for o mesmo alvo e ele ainda estiver vivo,
            // significa que simplesmente nao consegue atacar mais.
            if (newTarget == target && !newTarget.isDowned)
                break;

            target = newTarget;

            if (!CombatActions.TryBasicAttack(
                    context.unitStats,
                    target,
                    out string failureReason))
            {
                break;
            }

            yield return FinishAttack(context, target);
        }

        Debug.Log(
            "[AI] " + context.unitStats.gameObject.name +
            " terminou suas acoes com " +
            context.unitStats.currentActionPoints + " PA e " +
            context.unitStats.currentMovePoints + " PM."
        );
    }

    private bool IsValidContext(EnemyAITurnContext context)
    {
        return context != null &&
               context.unitStats != null &&
               context.movement != null &&
               context.unitManager != null &&
               context.pathfinding != null &&
               !context.unitStats.isDowned;
    }

    private void LogInvalidContext(EnemyAITurnContext context)
    {
        string unitName =
            context != null && context.unitStats != null
                ? context.unitStats.gameObject.name
                : "Enemy";

        Debug.LogWarning(
            "[AI] " + unitName +
            " esta sem referencias validas para executar AggressiveMeleeAI."
        );

        Debug.Log(
            "[AI] " + unitName +
            " nao encontrou acao valida."
        );
    }

    private UnitStats FindBestLivingPlayer(EnemyAITurnContext context)
    {
        List<UnitStats> players = context.unitManager.GetLivingPlayers();

        UnitStats best = null;
        int bestPathCost = int.MaxValue;
        int bestFallbackDistance = int.MaxValue;

        foreach (UnitStats player in players)
        {
            if (player == null || player.isDowned)
                continue;

            if (CombatActions.CanBasicAttack(context.unitStats, player, out string failureReason))
                return player;

            PathResult pathToAttackRange = FindFullPathToAttackRange(context, player);

            if (pathToAttackRange != null && pathToAttackRange.success)
            {
                if (pathToAttackRange.totalCost < bestPathCost)
                {
                    best = player;
                    bestPathCost = pathToAttackRange.totalCost;
                }

                continue;
            }

            int fallbackDistance = CombatActions.GetGridDistance(context.unitStats, player);

            if (best == null && fallbackDistance < bestFallbackDistance)
            {
                best = player;
                bestFallbackDistance = fallbackDistance;
            }
        }

        return best;
    }

    private IEnumerator FinishAttack(
        EnemyAITurnContext context,
        UnitStats target)
    {
        UnitStats attacker = context.unitStats;

        Debug.Log(
            "[AI] " + attacker.gameObject.name +
            " resolveu ataque basico contra " + target.gameObject.name +
            ". PA restante: " +
            attacker.currentActionPoints
        );

        if (context.afterAttackDelay > 0f)
            yield return new WaitForSeconds(context.afterAttackDelay);
    }

    private PathResult FindBestMovementPath(
        EnemyAITurnContext context,
        UnitStats target)
    {
        UnitStats mover = context.unitStats;

        if (mover.currentMovePoints <= 0)
            return null;

        PathResult fullPath = FindFullPathToAttackRange(context, target);

        if (fullPath == null || !fullPath.success)
            return null;

        return context.pathfinding.TrimPathToMovePoints(
            fullPath,
            mover.currentMovePoints
        );
    }

    private PathResult FindFullPathToAttackRange(
        EnemyAITurnContext context,
        UnitStats target)
    {
        UnitStats mover = context.unitStats;
        Vector3 startPosition = mover.transform.position;

        int targetX = Mathf.RoundToInt(target.transform.position.x);
        int targetZ = Mathf.RoundToInt(target.transform.position.z);
        int attackRange = mover.GetCurrentAttackRange();

        PathResult bestPath = null;

        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int z = -attackRange; z <= attackRange; z++)
            {
                int distanceToTarget = Mathf.Abs(x) + Mathf.Abs(z);

                if (distanceToTarget <= 0 || distanceToTarget > attackRange)
                    continue;

                int cellX = targetX + x;
                int cellZ = targetZ + z;

                Vector2Int cell =
                    new Vector2Int(cellX, cellZ);

                if (context.pathfinding.IsBlocked(
                        cell,
                        mover.gameObject))
                    continue;

                if (!CombatActions.CanAttackFromCell(mover, cell, target, out string failureReason))
                    continue;

                PathResult path =
                    context.pathfinding.FindPathResult(
                        startPosition,
                        cell,
                        mover
                    );

                if (path == null || !path.success || !path.HasSteps)
                    continue;

                if (bestPath == null || path.totalCost < bestPath.totalCost)
                {
                    bestPath = path;
                }
            }
        }

        return bestPath;
    }
}
