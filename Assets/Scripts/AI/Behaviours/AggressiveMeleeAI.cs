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

        UnitStats target = FindClosestLivingPlayer(context);

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
            context.unitStats.currentActionPoints >= context.unitStats.attackCost)
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
            while (context.unitStats.currentActionPoints >= context.unitStats.attackCost)
            {
                target = FindClosestLivingPlayer(context);

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
        List<Vector2Int> path = FindBestMovementPath(context, target);

        if (path != null && path.Count > 0)
        {
            int spentMovePoints = path.Count;

            context.movement.MoveAlongPath(path);

            while (context.movement.IsMoving())
                yield return null;

            Debug.Log(
                "[AI] " + context.unitStats.gameObject.name +
                " moveu " + spentMovePoints +
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
            context.unitStats.currentActionPoints >= context.unitStats.attackCost)
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
        while (context.unitStats.currentActionPoints >= context.unitStats.attackCost)
        {
            UnitStats newTarget = FindClosestLivingPlayer(context);

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

    private UnitStats FindClosestLivingPlayer(EnemyAITurnContext context)
    {
        List<UnitStats> players = context.unitManager.GetLivingPlayers();

        UnitStats closest = null;
        int closestDistance = int.MaxValue;

        foreach (UnitStats player in players)
        {
            if (player == null || player.isDowned)
                continue;

            int distance =
                CombatActions.GetGridDistance(context.unitStats, player);

            if (distance < closestDistance)
            {
                closest = player;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private IEnumerator FinishAttack(
        EnemyAITurnContext context,
        UnitStats target)
    {
        UnitStats attacker = context.unitStats;

        Debug.Log(
            "[AI] " + attacker.gameObject.name +
            " atacou " + target.gameObject.name +
            " causando " + attacker.attackDamage +
            " dano. PA restante: " +
            attacker.currentActionPoints
        );

        if (context.afterAttackDelay > 0f)
            yield return new WaitForSeconds(context.afterAttackDelay);
    }

    private List<Vector2Int> FindBestMovementPath(
        EnemyAITurnContext context,
        UnitStats target)
    {
        UnitStats mover = context.unitStats;

        if (mover.currentMovePoints <= 0)
            return null;

        Vector3 startPosition = mover.transform.position;

        int startX = Mathf.RoundToInt(startPosition.x);
        int startZ = Mathf.RoundToInt(startPosition.z);

        int targetX = Mathf.RoundToInt(target.transform.position.x);
        int targetZ = Mathf.RoundToInt(target.transform.position.z);

        List<Vector2Int> bestPath = null;

        int bestDistance =
            CombatActions.GetGridDistance(mover, target);

        for (int x = -mover.currentMovePoints;
             x <= mover.currentMovePoints;
             x++)
        {
            for (int z = -mover.currentMovePoints;
                 z <= mover.currentMovePoints;
                 z++)
            {
                int estimatedDistance =
                    Mathf.Abs(x) + Mathf.Abs(z);

                if (estimatedDistance <= 0 ||
                    estimatedDistance > mover.currentMovePoints)
                    continue;

                int cellX = startX + x;
                int cellZ = startZ + z;

                Vector2Int cell =
                    new Vector2Int(cellX, cellZ);

                if (context.pathfinding.IsBlocked(
                        cell,
                        mover.gameObject))
                    continue;

                List<Vector2Int> path =
                    context.pathfinding.FindPath(
                        startPosition,
                        cellX,
                        cellZ,
                        mover
                    );

                if (path == null ||
                    path.Count == 0 ||
                    path.Count > mover.currentMovePoints)
                    continue;

                int distanceToTarget =
                    Mathf.Abs(targetX - cellX) +
                    Mathf.Abs(targetZ - cellZ);

                if (
                    distanceToTarget < bestDistance ||
                    (
                        distanceToTarget == bestDistance &&
                        bestPath != null &&
                        path.Count < bestPath.Count
                    )
                )
                {
                    bestPath = path;
                    bestDistance = distanceToTarget;
                }
            }
        }

        return bestPath;
    }
}