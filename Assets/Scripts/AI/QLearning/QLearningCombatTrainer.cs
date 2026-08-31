using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class QLearningCombatTrainer : MonoBehaviour
{
    public enum CombatAction
    {
        Attack = 0,
        Approach = 1,
        Retreat = 2,
        Wait = 3,
        Dash = 4
    }

    [Header("Treinamento")]
    public bool trainOnStart = true;
    public int trainingEpisodes = 20000;
    public int evaluationEpisodes = 1000;
    public int maxRoundsPerBattle = 40;

    [Header("Mapa")]
    public int width = 8;
    public int height = 8;

    [Header("Combate")]
    public int maxHP = 3;

    [Tooltip("Alcance de ataque do aliado")]
    public int allyAttackRange = 2;

    [Tooltip("Alcance de ataque do inimigo")]
    public int enemyAttackRange = 1;

    [Tooltip("PM do aliado por turno")]
    public int allyMovePoints = 3;

    [Tooltip("PM do inimigo por turno")]
    public int enemyMovePoints = 3;

    [Header("Q-Learning")]
    [Range(0f, 1f)]
    public float learningRate = 0.15f;

    [Range(0f, 1f)]
    public float discountFactor = 0.95f;

    [Range(0f, 1f)]
    public float initialEpsilon = 1f;

    [Range(0f, 1f)]
    public float minimumEpsilon = 0.05f;

    public float epsilonDecay = 0.9995f;

    private const float TurnReward = -1f;
    private const float HitReward = 15f;
    private const float KillReward = 100f;
    private const float DeathPenalty = -100f;
    private const float InvalidActionPenalty = -5f;
    private const float DrawPenalty = -50f;
    private const int AllyActionCount = 4;
    private const int EnemyActionCount = 5;

    private float epsilon;

    private readonly Dictionary<string, float[]> allyQTable =
        new Dictionary<string, float[]>();

    private readonly Dictionary<string, float[]> enemyQTable =
        new Dictionary<string, float[]>();

    private readonly HashSet<Vector2Int> blocked =
        new HashSet<Vector2Int>();

    private System.Random random;

    private Agent ally;
    private Agent enemy;
    private bool enemyDashAvailable;

    private class Agent
    {
        public Vector2Int position;
        public int hp;

        public Agent(Vector2Int position, int hp)
        {
            this.position = position;
            this.hp = hp;
        }
    }

    private struct ActionResult
    {
        public bool hit;
        public bool invalid;
        public bool dashUsed;
        public int movedTiles;
        public int distanceBefore;
        public int distanceAfter;
        public Vector2Int positionBefore;
        public Vector2Int positionAfter;
        public int targetHpBefore;
        public int targetHpAfter;
    }

    private struct EvaluationResult
    {
        public int allyWins;
        public int enemyWins;
        public int draws;
        public int invalidActions;
        public int dashesUsed;
        public int dashHits;
        public int invalidDashAttempts;
        public int enemyWinsWithDash;

        public float averageRounds;
        public float allyAverageReward;
        public float enemyAverageReward;
    }

    private void Start()
    {
        if (trainOnStart)
            RunExperiment();
    }

    [ContextMenu("Executar Experimento")]
    public void RunExperiment()
    {
        random = new System.Random(42);

        allyQTable.Clear();
        enemyQTable.Clear();

        BuildObstacleMap();

        Debug.Log("========================================");
        Debug.Log("TESTE Q-LEARNING + BFS + OBSTÁCULOS");
        Debug.Log("========================================");

        Debug.Log(
            $"Mapa: {width}x{height} | " +
            $"Ally Range: {allyAttackRange} | " +
            $"Enemy Range: {enemyAttackRange} | " +
            $"Ally PM: {allyMovePoints} | " +
            $"Enemy PM: {enemyMovePoints}"
        );

        Debug.Log("Avaliando agentes ANTES do treinamento...");

        EvaluationResult before = Evaluate(false);

        PrintEvaluation(
            "ANTES DO TREINAMENTO",
            before
        );

        Debug.Log("INICIANDO TREINAMENTO...");

        Train();

        Debug.Log("TREINAMENTO FINALIZADO.");

        EvaluationResult after = Evaluate(true);

        PrintEvaluation(
            "DEPOIS DO TREINAMENTO",
            after
        );

        PrintPolicies();

        Debug.Log("========================================");
        Debug.Log("BATALHA DE DEMONSTRAÇÃO");
        Debug.Log("========================================");

        RunDemonstration();
    }

    // =========================================================
    // MAPA / OBSTÁCULOS
    // =========================================================

    private void BuildObstacleMap()
    {
        blocked.Clear();

        /*
            Parede central:

            . . . # . . . .
            . . . # . . . .
            . . . # . . . .
            . . . # . . . .
            . . . # . . . .
            . . . # . . . .

            Existem passagens nas pontas.

            Isso força o inimigo a perceber que
            não pode simplesmente andar em linha reta.
        */

        int wallX = width / 2;

        for (int y = 1; y < height - 1; y++)
        {
            blocked.Add(
                new Vector2Int(wallX, y)
            );
        }
    }

    // =========================================================
    // TREINAMENTO
    // =========================================================

    private void Train()
    {
        epsilon = initialEpsilon;

        float allyRewardWindow = 0f;
        float enemyRewardWindow = 0f;

        for (
            int episode = 1;
            episode <= trainingEpisodes;
            episode++
        )
        {
            ResetBattle();

            float allyEpisodeReward = 0f;
            float enemyEpisodeReward = 0f;

            string previousAllyState = null;
            CombatAction previousAllyAction = CombatAction.Wait;

            string previousEnemyState = null;
            CombatAction previousEnemyAction = CombatAction.Wait;

            for (
                int round = 0;
                round < maxRoundsPerBattle;
                round++
            )
            {
                // =================================================
                // TURNO DO ALIADO
                // =================================================

                if (BattleFinished())
                    break;

                string allyState =
                    GetState(
                        ally,
                        enemy,
                        allyAttackRange,
                        enemyAttackRange
                    );

                CombatAction allyAction =
                    ChooseAction(
                        allyQTable,
                        allyState,
                        epsilon,
                        AllyActionCount
                    );

                previousAllyState = allyState;
                previousAllyAction = allyAction;

                ActionResult allyResult =
                    ApplyAction(
                        ally,
                        enemy,
                        allyAction,
                        allyAttackRange,
                        allyMovePoints
                    );

                float allyReward =
                    CalculateAllyReward(
                        allyResult,
                        allyAction
                    );

                bool terminal =
                    BattleFinished();

                if (enemy.hp <= 0)
                    allyReward += KillReward;

                string allyNextState =
                    terminal
                        ? null
                        : GetState(
                            ally,
                            enemy,
                            allyAttackRange,
                            enemyAttackRange
                        );

                UpdateQ(
                    allyQTable,
                    allyState,
                    allyAction,
                    allyReward,
                    allyNextState,
                    terminal
                );

                allyEpisodeReward += allyReward;

                if (enemy.hp <= 0)
                {
                    // Inimigo morreu:
                    // pune a última decisão dele.
                    if (previousEnemyState != null)
                    {
                        UpdateQ(
                            enemyQTable,
                            previousEnemyState,
                            previousEnemyAction,
                            DeathPenalty,
                            null,
                            true,
                            EnemyActionCount
                        );

                        enemyEpisodeReward += DeathPenalty;
                    }

                    break;
                }

                // =================================================
                // TURNO DO INIMIGO
                // =================================================

                string enemyState =
                    GetState(
                        enemy,
                        ally,
                        enemyAttackRange,
                        allyAttackRange,
                        true,
                        enemyDashAvailable
                    );

                CombatAction enemyAction =
                    ChooseAction(
                        enemyQTable,
                        enemyState,
                        epsilon,
                        EnemyActionCount
                    );

                previousEnemyState = enemyState;
                previousEnemyAction = enemyAction;

                ActionResult enemyResult =
                    ApplyAction(
                        enemy,
                        ally,
                        enemyAction,
                        enemyAttackRange,
                        enemyMovePoints,
                        enemyDashAvailable
                    );

                if (enemyResult.dashUsed)
                    enemyDashAvailable = false;

                float enemyReward =
                    CalculateEnemyReward(
                        enemyResult,
                        enemyAction
                    );

                terminal =
                    BattleFinished();

                if (ally.hp <= 0)
                    enemyReward += KillReward;

                string enemyNextState =
                    terminal
                        ? null
                        : GetState(
                            enemy,
                            ally,
                            enemyAttackRange,
                            allyAttackRange,
                            true,
                            enemyDashAvailable
                        );

                UpdateQ(
                    enemyQTable,
                    enemyState,
                    enemyAction,
                    enemyReward,
                    enemyNextState,
                    terminal,
                    EnemyActionCount
                );

                enemyEpisodeReward += enemyReward;

                if (ally.hp <= 0)
                {
                    // Aliado morreu:
                    // pune a última decisão dele.
                    if (previousAllyState != null)
                    {
                        UpdateQ(
                            allyQTable,
                            previousAllyState,
                            previousAllyAction,
                            DeathPenalty,
                            null,
                            true
                        );

                        allyEpisodeReward += DeathPenalty;
                    }

                    break;
                }
            }

            if (
                ally.hp > 0 &&
                enemy.hp > 0
            )
            {
                if (previousAllyState != null)
                {
                    UpdateQ(
                        allyQTable,
                        previousAllyState,
                        previousAllyAction,
                        DrawPenalty,
                        null,
                        true
                    );

                    allyEpisodeReward += DrawPenalty;
                }

                if (previousEnemyState != null)
                {
                    UpdateQ(
                        enemyQTable,
                        previousEnemyState,
                        previousEnemyAction,
                        DrawPenalty,
                        null,
                        true,
                        EnemyActionCount
                    );

                    enemyEpisodeReward += DrawPenalty;
                }
            }

            allyRewardWindow += allyEpisodeReward;
            enemyRewardWindow += enemyEpisodeReward;

            epsilon *= epsilonDecay;

            epsilon =
                Mathf.Max(
                    epsilon,
                    minimumEpsilon
                );

            if (episode % 1000 == 0)
            {
                Debug.Log(
                    $"Episódio {episode}/{trainingEpisodes} | " +
                    $"Epsilon: {epsilon:F3} | " +
                    $"Reward Ally: {allyRewardWindow / 1000f:F2} | " +
                    $"Reward Enemy: {enemyRewardWindow / 1000f:F2}"
                );

                allyRewardWindow = 0f;
                enemyRewardWindow = 0f;
            }
        }
    }

    // =========================================================
    // ESTADO
    // =========================================================

    private string GetState(
        Agent self,
        Agent opponent,
        int ownRange,
        int opponentRange,
        bool includeDashState = false,
        bool dashAvailable = false
    )
    {
        int manhattan =
            ManhattanDistance(
                self.position,
                opponent.position
            );

        int pathDistance =
            GetPathDistance(
                self.position,
                opponent.position
            );

        if (pathDistance < 0)
            pathDistance = 20;

        int distanceBucket =
            Mathf.Min(pathDistance, 6);

        bool canAttack =
            manhattan <= ownRange;

        bool threatened =
            manhattan <= opponentRange;

        string state =
            $"D{distanceBucket}" +
            $"_HP{self.hp}" +
            $"_EHP{opponent.hp}" +
            $"_ATK{BoolInt(canAttack)}" +
            $"_THREAT{BoolInt(threatened)}";

        if (includeDashState)
        {
            state +=
                $"_LINE{BoolInt(HasClearLineForDash(self.position, opponent.position))}" +
                $"_DASH{BoolInt(dashAvailable)}";
        }

        return state;
    }

    private int BoolInt(bool value)
    {
        return value ? 1 : 0;
    }

    // =========================================================
    // DECISÃO Q-LEARNING
    // =========================================================

    private CombatAction ChooseAction(
        Dictionary<string, float[]> table,
        string state,
        float exploration,
        int actionCount
    )
    {
        EnsureState(
            table,
            state,
            actionCount
        );

        if (random.NextDouble() < exploration)
        {
            return
                (CombatAction)random.Next(
                    0,
                    actionCount
                );
        }

        return GetBestAction(
            table,
            state,
            actionCount
        );
    }

    private CombatAction GetBestAction(
        Dictionary<string, float[]> table,
        string state,
        int actionCount
    )
    {
        EnsureState(
            table,
            state,
            actionCount
        );

        float[] values =
            table[state];

        float bestValue =
            values[0];

        List<int> bestActions =
            new List<int>();

        bestActions.Add(0);

        for (
            int i = 1;
            i < values.Length;
            i++
        )
        {
            if (values[i] > bestValue)
            {
                bestValue = values[i];

                bestActions.Clear();

                bestActions.Add(i);
            }
            else if (
                Mathf.Approximately(
                    values[i],
                    bestValue
                )
            )
            {
                bestActions.Add(i);
            }
        }

        int selected =
            bestActions[
                random.Next(
                    bestActions.Count
                )
            ];

        return
            (CombatAction)selected;
    }

    // =========================================================
    // EXECUÇÃO DAS AÇÕES
    // =========================================================

    private ActionResult ApplyAction(
        Agent actor,
        Agent target,
        CombatAction action,
        int attackRange,
        int movePoints,
        bool canUseDash = false
    )
    {
        ActionResult result =
            new ActionResult();

        result.positionBefore =
            actor.position;

        result.targetHpBefore =
            target.hp;

        result.distanceBefore =
            ManhattanDistance(
                actor.position,
                target.position
            );

        switch (action)
        {
            // =============================================
            // ATAQUE
            // =============================================

            case CombatAction.Attack:

                if (
                    ManhattanDistance(
                        actor.position,
                        target.position
                    )
                    <= attackRange
                )
                {
                    target.hp--;

                    result.hit = true;
                }
                else
                {
                    result.invalid = true;
                }

                break;

            // =============================================
            // APROXIMAR
            // =============================================

            case CombatAction.Approach:

                /*
                    IMPORTANTE:

                    Aqui o BFS calcula TODO o caminho
                    necessário.

                    Só depois aplicamos o limite de PM.
                */

                List<Vector2Int> path =
                    FindPathToAttackRange(
                        actor.position,
                        target.position,
                        attackRange
                    );

                if (
                    path == null ||
                    path.Count == 0
                )
                {
                    result.invalid = true;
                    break;
                }

                int steps =
                    GetExecutableStepCount(
                        path,
                        movePoints
                    );

                if (steps <= 0)
                {
                    result.invalid = true;
                    break;
                }

                actor.position =
                    path[steps - 1];

                result.movedTiles =
                    GetPathCost(path, steps);

                break;

            // =============================================
            // RECUAR
            // =============================================

            case CombatAction.Retreat:

                Vector2Int retreat =
                    FindBestRetreatPosition(
                        actor.position,
                        target.position,
                        movePoints
                    );

                if (
                    retreat ==
                    actor.position
                )
                {
                    result.invalid = true;
                }
                else
                {
                    result.movedTiles =
                        GetPathDistance(
                            actor.position,
                            retreat
                        );

                    actor.position =
                        retreat;
                }

                break;

            // =============================================
            // ESPERAR
            // =============================================

            case CombatAction.Wait:

                break;

            // =============================================
            // DASH
            // =============================================

            case CombatAction.Dash:

                if (
                    canUseDash &&
                    TryGetDashDestination(
                        actor.position,
                        target.position,
                        out Vector2Int dashDestination
                    )
                )
                {
                    actor.position =
                        dashDestination;

                    target.hp--;

                    result.hit = true;
                    result.dashUsed = true;
                }
                else
                {
                    result.invalid = true;
                }

                break;
        }

        result.distanceAfter =
            ManhattanDistance(
                actor.position,
                target.position
            );

        result.positionAfter =
            actor.position;

        result.targetHpAfter =
            target.hp;

        return result;
    }

    // =========================================================
    // RECOMPENSA ALIADO
    // =========================================================

    private float CalculateAllyReward(
        ActionResult result,
        CombatAction action
    )
    {
        float reward = TurnReward;

        int distance =
            ManhattanDistance(
                ally.position,
                enemy.position
            );

        // Acertar
        if (result.hit)
            reward += HitReward;

        // Distância perfeita:
        // aliado consegue atacar
        // e inimigo melee não.
        if (distance == 2)
            reward += 1f;

        // Inimigo encostou
        if (distance <= 1)
            reward -= 5f;

        if (result.invalid)
            reward += InvalidActionPenalty;

        return reward;
    }

    // =========================================================
    // RECOMPENSA INIMIGO
    // =========================================================

    private float CalculateEnemyReward(
        ActionResult result,
        CombatAction action
    )
    {
        float reward = TurnReward;

        // Acertou o aliado
        if (result.hit)
            reward += HitReward;

        // Aproximou
        if (
            result.distanceAfter <
            result.distanceBefore
        )
        {
            reward += 1f;
        }

        // Se afastou estando longe
        if (
            result.distanceBefore > 1 &&
            result.distanceAfter >
            result.distanceBefore
        )
        {
            reward -= 2f;
        }

        if (result.invalid)
            reward += InvalidActionPenalty;

        return reward;
    }

    // =========================================================
    // DASH
    // =========================================================

    private bool TryGetDashDestination(
        Vector2Int start,
        Vector2Int target,
        out Vector2Int destination
    )
    {
        destination =
            start;

        if (
            start.x != target.x &&
            start.y != target.y
        )
        {
            return false;
        }

        Vector2Int direction =
            GetLineDirection(
                start,
                target
            );

        if (direction == Vector2Int.zero)
            return false;

        Vector2Int next =
            start + direction;

        while (next != target)
        {
            if (
                !InsideMap(next) ||
                blocked.Contains(next)
            )
            {
                return false;
            }

            if (next + direction == target)
            {
                destination =
                    next;
            }

            next += direction;
        }

        return
            destination != start &&
            destination != target &&
            InsideMap(destination) &&
            !blocked.Contains(destination);
    }

    private bool HasClearLineForDash(
        Vector2Int start,
        Vector2Int target
    )
    {
        return
            TryGetDashDestination(
                start,
                target,
                out _
            );
    }

    private Vector2Int GetLineDirection(
        Vector2Int start,
        Vector2Int target
    )
    {
        if (start.x == target.x)
        {
            return new Vector2Int(
                0,
                Math.Sign(target.y - start.y)
            );
        }

        if (start.y == target.y)
        {
            return new Vector2Int(
                Math.Sign(target.x - start.x),
                0
            );
        }

        return Vector2Int.zero;
    }

    // =========================================================
    // BFS - CAMINHO COMPLETO
    // =========================================================

    private List<Vector2Int> FindPathToAttackRange(
        Vector2Int start,
        Vector2Int target,
        int desiredRange
    )
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int>
            cameFrom =
                new Dictionary<Vector2Int, Vector2Int>();

        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int found =
            new Vector2Int();

        bool hasFound = false;

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            if (
                current != target &&
                ManhattanDistance(
                    current,
                    target
                )
                <= desiredRange
            )
            {
                found = current;
                hasFound = true;
                break;
            }

            foreach (
                Vector2Int next
                in GetNeighbours(current)
            )
            {
                if (visited.Contains(next))
                    continue;

                if (blocked.Contains(next))
                    continue;

                // Não pode andar em cima do outro agente
                if (next == target)
                    continue;

                visited.Add(next);

                cameFrom[next] =
                    current;

                queue.Enqueue(next);
            }
        }

        if (!hasFound)
            return null;

        List<Vector2Int> path =
            new List<Vector2Int>();

        Vector2Int step =
            found;

        while (step != start)
        {
            path.Add(step);

            step =
                cameFrom[step];
        }

        path.Reverse();

        return path;
    }

    // =========================================================
    // RECUAR
    // =========================================================

    private Vector2Int FindBestRetreatPosition(
        Vector2Int start,
        Vector2Int target,
        int movePoints
    )
    {
        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, int> distance =
            new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);

        distance[start] = 0;

        Vector2Int best =
            start;

        int bestTargetDistance =
            GetPathDistance(
                start,
                target
            );

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            int movementCost =
                distance[current];

            int targetDistance =
                GetPathDistance(
                    current,
                    target
                );

            if (
                targetDistance >
                bestTargetDistance
            )
            {
                bestTargetDistance =
                    targetDistance;

                best =
                    current;
            }

            if (
                movementCost >=
                movePoints
            )
            {
                continue;
            }

            foreach (
                Vector2Int next
                in GetNeighbours(current)
            )
            {
                if (
                    distance.ContainsKey(next)
                )
                {
                    continue;
                }

                if (blocked.Contains(next))
                    continue;

                if (next == target)
                    continue;

                distance[next] =
                    movementCost + 1;

                queue.Enqueue(next);
            }
        }

        return best;
    }

    // =========================================================
    // DISTÂNCIA REAL PELO MAPA
    // =========================================================

    private int GetPathDistance(
        Vector2Int start,
        Vector2Int target
    )
    {
        if (start == target)
            return 0;

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, int> distance =
            new Dictionary<Vector2Int, int>();

        queue.Enqueue(start);

        distance[start] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            foreach (
                Vector2Int next
                in GetNeighbours(current)
            )
            {
                if (
                    distance.ContainsKey(next)
                )
                {
                    continue;
                }

                if (blocked.Contains(next))
                    continue;

                int newDistance =
                    distance[current] + 1;

                if (next == target)
                    return newDistance;

                distance[next] =
                    newDistance;

                queue.Enqueue(next);
            }
        }

        return -1;
    }

    private int GetExecutableStepCount(
        List<Vector2Int> path,
        int movePoints
    )
    {
        if (path == null)
            return 0;

        int cost = 0;
        int steps = 0;

        for (int i = 0; i < path.Count; i++)
        {
            int stepCost =
                i == 0
                    ? 1
                    : GetMovementCost(path[i - 1], path[i]);

            if (cost + stepCost > movePoints)
                break;

            cost += stepCost;
            steps++;
        }

        return steps;
    }

    private int GetPathCost(
        List<Vector2Int> path,
        int maxSteps
    )
    {
        if (path == null)
            return 0;

        int cost = 0;
        int steps = Mathf.Min(maxSteps, path.Count);

        for (int i = 0; i < steps; i++)
        {
            cost +=
                i == 0
                    ? 1
                    : GetMovementCost(path[i - 1], path[i]);
        }

        return cost;
    }

    private int GetMovementCost(
        Vector2Int current,
        Vector2Int next
    )
    {
        int deltaX =
            Mathf.Abs(next.x - current.x);

        int deltaY =
            Mathf.Abs(next.y - current.y);

        if (deltaX == 1 && deltaY == 1)
            return 2;

        if (deltaX + deltaY == 1)
            return 1;

        return 0;
    }

    // =========================================================
    // VIZINHOS
    // =========================================================

    private IEnumerable<Vector2Int>
        GetNeighbours(
            Vector2Int position
        )
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (
            Vector2Int direction
            in directions
        )
        {
            Vector2Int next =
                position +
                direction;

            if (InsideMap(next))
                yield return next;
        }
    }

    // =========================================================
    // Q TABLE
    // =========================================================

    private void UpdateQ(
        Dictionary<string, float[]> table,
        string state,
        CombatAction action,
        float reward,
        string nextState,
        bool terminal,
        int actionCount = AllyActionCount
    )
    {
        EnsureState(
            table,
            state,
            actionCount
        );

        int index =
            (int)action;

        float current =
            table[state][index];

        float future = 0f;

        if (
            !terminal &&
            nextState != null
        )
        {
            EnsureState(
                table,
                nextState,
                actionCount
            );

            future =
                MaxQ(
                    table[nextState]
                );
        }

        float target =
            reward +
            discountFactor *
            future;

        table[state][index] =
            current +
            learningRate *
            (target - current);
    }

    private void EnsureState(
        Dictionary<string, float[]> table,
        string state,
        int actionCount
    )
    {
        if (
            !table.ContainsKey(state)
        )
        {
            table[state] =
                new float[actionCount];
        }
    }

    private float MaxQ(
        float[] values
    )
    {
        float max =
            values[0];

        for (
            int i = 1;
            i < values.Length;
            i++
        )
        {
            if (values[i] > max)
                max = values[i];
        }

        return max;
    }

    // =========================================================
    // RESET DA BATALHA
    // =========================================================

    private void ResetBattle()
    {
        /*
            Inimigo começa de um lado da parede.
            Aliado começa do outro.

            Isso força o obstáculo a participar
            do treinamento.
        */

        Vector2Int enemyPosition;
        Vector2Int allyPosition;

        do
        {
            enemyPosition =
                new Vector2Int(
                    random.Next(
                        0,
                        Mathf.Max(
                            1,
                            width / 2 - 1
                        )
                    ),
                    random.Next(
                        0,
                        height
                    )
                );
        }
        while (
            blocked.Contains(
                enemyPosition
            )
        );

        do
        {
            allyPosition =
                new Vector2Int(
                    random.Next(
                        width / 2 + 1,
                        width
                    ),
                    random.Next(
                        0,
                        height
                    )
                );
        }
        while (
            blocked.Contains(
                allyPosition
            )
        );

        enemy =
            new Agent(
                enemyPosition,
                maxHP
            );

        ally =
            new Agent(
                allyPosition,
                maxHP
            );

        enemyDashAvailable = true;
    }

    // =========================================================
    // AVALIAÇÃO
    // =========================================================

    private EvaluationResult Evaluate(
        bool trained
    )
    {
        EvaluationResult result =
            new EvaluationResult();

        float roundsTotal = 0f;

        for (
            int episode = 0;
            episode < evaluationEpisodes;
            episode++
        )
        {
            ResetBattle();

            float allyReward = 0f;
            float enemyReward = 0f;

            int roundsPlayed = 0;
            bool enemyUsedDashThisBattle = false;

            for (
                int round = 0;
                round < maxRoundsPerBattle;
                round++
            )
            {
                if (BattleFinished())
                    break;

                roundsPlayed++;

                string allyState =
                    GetState(
                        ally,
                        enemy,
                        allyAttackRange,
                        enemyAttackRange
                    );

                CombatAction allyAction =
                    trained
                        ? GetBestAction(
                            allyQTable,
                            allyState,
                            AllyActionCount
                        )
                        : (CombatAction)
                            random.Next(AllyActionCount);

                ActionResult allyResult =
                    ApplyAction(
                        ally,
                        enemy,
                        allyAction,
                        allyAttackRange,
                        allyMovePoints
                    );

                allyReward +=
                    CalculateAllyReward(
                        allyResult,
                        allyAction
                    );

                if (allyResult.invalid)
                    result.invalidActions++;

                if (enemy.hp <= 0)
                {
                    allyReward += KillReward;
                    enemyReward += DeathPenalty;
                    break;
                }

                string enemyState =
                    GetState(
                        enemy,
                        ally,
                        enemyAttackRange,
                        allyAttackRange,
                        true,
                        enemyDashAvailable
                    );

                CombatAction enemyAction =
                    trained
                        ? GetBestAction(
                            enemyQTable,
                            enemyState,
                            EnemyActionCount
                        )
                        : (CombatAction)
                            random.Next(EnemyActionCount);

                ActionResult enemyResult =
                    ApplyAction(
                        enemy,
                        ally,
                        enemyAction,
                        enemyAttackRange,
                        enemyMovePoints,
                        enemyDashAvailable
                    );

                if (enemyAction == CombatAction.Dash)
                {
                    if (enemyResult.dashUsed)
                    {
                        result.dashesUsed++;
                        enemyUsedDashThisBattle = true;

                        if (enemyResult.hit)
                            result.dashHits++;
                    }
                    else if (enemyResult.invalid)
                    {
                        result.invalidDashAttempts++;
                    }
                }

                if (enemyResult.dashUsed)
                    enemyDashAvailable = false;

                enemyReward +=
                    CalculateEnemyReward(
                        enemyResult,
                        enemyAction
                    );

                if (enemyResult.invalid)
                    result.invalidActions++;

                if (ally.hp <= 0)
                {
                    enemyReward += KillReward;
                    allyReward += DeathPenalty;
                    break;
                }
            }

            if (
                ally.hp > 0 &&
                enemy.hp <= 0
            )
            {
                result.allyWins++;
            }
            else if (
                enemy.hp > 0 &&
                ally.hp <= 0
            )
            {
                result.enemyWins++;

                if (enemyUsedDashThisBattle)
                    result.enemyWinsWithDash++;
            }
            else
            {
                result.draws++;
                allyReward += DrawPenalty;
                enemyReward += DrawPenalty;
            }

            roundsTotal +=
                roundsPlayed;

            result.allyAverageReward +=
                allyReward;

            result.enemyAverageReward +=
                enemyReward;
        }

        result.averageRounds =
            roundsTotal /
            evaluationEpisodes;

        result.allyAverageReward /=
            evaluationEpisodes;

        result.enemyAverageReward /=
            evaluationEpisodes;

        return result;
    }

private void PrintEvaluation(
    string title,
    EvaluationResult result
)
{
    Debug.Log($"===== {title} =====");

    Debug.Log($"Vitórias Aliado: {result.allyWins}");
    Debug.Log($"Vitórias Inimigo: {result.enemyWins}");
    Debug.Log($"Empates: {result.draws}");
    Debug.Log($"Dashes utilizados: {result.dashesUsed}");
    Debug.Log($"Dashes com dano: {result.dashHits}");
    Debug.Log($"Tentativas invalidas de Dash: {result.invalidDashAttempts}");
    Debug.Log($"Vitorias Inimigo com Dash: {result.enemyWinsWithDash}");
    Debug.Log($"Ações inválidas: {result.invalidActions}");
    Debug.Log($"Rounds médios: {result.averageRounds:F2}");
    Debug.Log($"Reward médio Ally: {result.allyAverageReward:F2}");
    Debug.Log($"Reward médio Enemy: {result.enemyAverageReward:F2}");

    Debug.Log("==============================");
}

    // =========================================================
    // MOSTRAR POLÍTICA
    // =========================================================

    private void PrintPolicies()
    {
        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "ALGUMAS DECISÕES APRENDIDAS"
        );

        Debug.Log(
            "========================================"
        );

        PrintTableSummary(
            "ALIADO",
            allyQTable,
            AllyActionCount
        );

        PrintTableSummary(
            "INIMIGO",
            enemyQTable,
            EnemyActionCount
        );
    }

    private void PrintTableSummary(
        string name,
        Dictionary<string, float[]> table,
        int actionCount
    )
    {
        int attack = 0;
        int approach = 0;
        int retreat = 0;
        int wait = 0;
        int dash = 0;

        foreach (
            KeyValuePair<string, float[]>
            entry in table
        )
        {
            CombatAction best =
                GetBestAction(
                    table,
                    entry.Key,
                    actionCount
                );

            switch (best)
            {
                case CombatAction.Attack:
                    attack++;
                    break;

                case CombatAction.Approach:
                    approach++;
                    break;

                case CombatAction.Retreat:
                    retreat++;
                    break;

                case CombatAction.Wait:
                    wait++;
                    break;

                case CombatAction.Dash:
                    dash++;
                    break;
            }
        }

        string summary =
            $"{name} - Estados aprendidos: {table.Count}\n" +
            $"Attack: {attack}\n" +
            $"Approach: {approach}\n" +
            $"Retreat: {retreat}\n" +
            $"Wait: {wait}";

        if (actionCount > AllyActionCount)
            summary += $"\nDash: {dash}";

        Debug.Log(summary);
    }

    // =========================================================
    // DEMONSTRAÇÃO
    // =========================================================

    private void RunDemonstration()
    {
        ResetBattle();

        Debug.Log(
            "MAPA INICIAL:\n" +
            MapToString()
        );

        for (
            int round = 1;
            round <= maxRoundsPerBattle;
            round++
        )
        {
            if (BattleFinished())
                break;

            Debug.Log(
                $"\nROUND {round}\n" +
                $"Ally HP: {ally.hp} Pos: {ally.position}\n" +
                $"Enemy HP: {enemy.hp} Pos: {enemy.position}"
            );

            // =========================
            // ALLY
            // =========================

            string allyState =
                GetState(
                    ally,
                    enemy,
                    allyAttackRange,
                    enemyAttackRange
                );

            CombatAction allyAction =
                GetBestAction(
                    allyQTable,
                    allyState,
                    AllyActionCount
                );

            Debug.Log(
                $"ALLY decidiu: {allyAction}"
            );

            ApplyAction(
                ally,
                enemy,
                allyAction,
                allyAttackRange,
                allyMovePoints
            );

            Debug.Log(
                MapToString()
            );

            if (enemy.hp <= 0)
                break;

            // =========================
            // ENEMY
            // =========================

            string enemyState =
                GetState(
                    enemy,
                    ally,
                    enemyAttackRange,
                    allyAttackRange,
                    true,
                    enemyDashAvailable
                );

            CombatAction enemyAction =
                GetBestAction(
                    enemyQTable,
                    enemyState,
                    EnemyActionCount
                );

            Debug.Log(
                $"ENEMY decidiu: {enemyAction}"
            );

            ActionResult enemyResult =
                ApplyAction(
                    enemy,
                    ally,
                    enemyAction,
                    enemyAttackRange,
                    enemyMovePoints,
                    enemyDashAvailable
                );

            if (enemyAction == CombatAction.Dash)
            {
                if (enemyResult.dashUsed)
                {
                    enemyDashAvailable = false;

                    Debug.Log(
                        "Dash executado\n" +
                        $"Enemy: {enemyResult.positionBefore} -> {enemyResult.positionAfter}\n" +
                        $"Ally HP: {enemyResult.targetHpBefore} -> {enemyResult.targetHpAfter}"
                    );
                }
                else
                {
                    Debug.Log("Dash invalido");
                }
            }
            else if (enemyResult.dashUsed)
            {
                enemyDashAvailable = false;
            }

            Debug.Log(
                MapToString()
            );
        }

        if (enemy.hp <= 0)
        {
            Debug.Log(
                "RESULTADO: ALIADO VENCEU"
            );
        }
        else if (ally.hp <= 0)
        {
            Debug.Log(
                "RESULTADO: INIMIGO VENCEU"
            );
        }
        else
        {
            Debug.Log(
                "RESULTADO: EMPATE"
            );
        }
    }

    // =========================================================
    // MAPA NO CONSOLE
    // =========================================================

    private string MapToString()
    {
        StringBuilder builder =
            new StringBuilder();

        for (
            int y = height - 1;
            y >= 0;
            y--
        )
        {
            for (
                int x = 0;
                x < width;
                x++
            )
            {
                Vector2Int position =
                    new Vector2Int(
                        x,
                        y
                    );

                if (
                    position ==
                    ally.position
                )
                {
                    builder.Append("A ");
                }
                else if (
                    position ==
                    enemy.position
                )
                {
                    builder.Append("E ");
                }
                else if (
                    blocked.Contains(
                        position
                    )
                )
                {
                    builder.Append("# ");
                }
                else
                {
                    builder.Append(". ");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    // =========================================================
    // UTILIDADES
    // =========================================================

    private int ManhattanDistance(
        Vector2Int a,
        Vector2Int b
    )
    {
        return
            Mathf.Abs(a.x - b.x) +
            Mathf.Abs(a.y - b.y);
    }

    private bool InsideMap(
        Vector2Int position
    )
    {
        return
            position.x >= 0 &&
            position.x < width &&
            position.y >= 0 &&
            position.y < height;
    }

    private bool BattleFinished()
    {
        return
            ally.hp <= 0 ||
            enemy.hp <= 0;
    }
}
