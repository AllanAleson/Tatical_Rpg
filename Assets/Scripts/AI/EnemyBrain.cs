using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public UnitStats unitStats;
    public UnitMovement movement;
    public EnemyAIBehaviour behaviour;
    public TurnManager turnManager;
    public UnitManager unitManager;
    public Pathfinding pathfinding;
    public TacticalCameraController tacticalCameraController;

    public float decisionDelay = 0.35f;
    public float afterMoveDelay = 0.35f;
    public float afterAttackDelay = 0.35f;

    private TurnManager.ActionExecution turnAction;
    private readonly Stack<IEnumerator> executionStack = new Stack<IEnumerator>();

    void Awake()
    {
        if (unitStats == null)
            unitStats = GetComponent<UnitStats>();

        if (movement == null)
            movement = GetComponent<UnitMovement>();

        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();

        if (unitManager == null)
            unitManager = UnitManager.Instance;

        if (pathfinding == null)
            pathfinding = FindAnyObjectByType<Pathfinding>();

        if (tacticalCameraController == null)
            tacticalCameraController = FindAnyObjectByType<TacticalCameraController>();
    }

    void OnEnable()
    {
        if (turnManager != null)
            turnManager.OnTurnStarted += HandleTurnStarted;
    }

    void OnDisable()
    {
        if (turnManager != null)
            turnManager.OnTurnStarted -= HandleTurnStarted;
        CancelExecution();
    }

    void Update()
    {
        // Poll even during WaitForSeconds/camera waits so cancellation is prompt.
        if (turnAction != null && !turnAction.IsAuthorized)
            CancelExecution();
    }

    private void HandleTurnStarted(UnitStats turnUnit)
    {
        if (turnUnit == null || turnUnit != unitStats)
            return;

        if (unitStats == null || unitStats.team != UnitStats.Team.Enemy || unitStats.isDowned)
            return;

        if (turnAction != null || turnManager == null ||
            !turnManager.TryBeginAction(unitStats, out var action))
            return;

        turnAction = action;
        StartCoroutine(ExecuteEnemyTurn(action));
    }

    private IEnumerator ExecuteEnemyTurn(TurnManager.ActionExecution action)
    {
        int version = turnManager.TurnVersion;
        executionStack.Push(PerformEnemyTurn(action));
        try
        {
            // Drive nested iterators here so every resume is authorized and all
            // iterators can be disposed on cancellation, disable, or exception.
            while (action.IsAuthorized && executionStack.Count > 0)
            {
                IEnumerator routine = executionStack.Peek();
                if (!routine.MoveNext())
                {
                    executionStack.Pop();
                    (routine as System.IDisposable)?.Dispose();
                    continue;
                }
                if (routine.Current is IEnumerator nested)
                    executionStack.Push(nested);
                else
                    yield return routine.Current;
            }
        }
        finally
        {
            FinishExecution(action);
        }

        if (turnManager != null && turnManager.TurnVersion == version &&
            turnManager.IsCurrentUnit(unitStats))
            turnManager.TryEndTurn(unitStats);
    }

    private IEnumerator PerformEnemyTurn(TurnManager.ActionExecution action)
    {

        if (tacticalCameraController != null)
            yield return tacticalCameraController.WaitForCurrentTurnFocusInitialDelay(unitStats);

        if (!action.IsAuthorized)
            yield break;

        Debug.Log("[AI] " + unitStats.gameObject.name + " iniciou turno.");

        if (behaviour != null)
        {
            EnemyAITurnContext context = new EnemyAITurnContext
            {
                brain = this,
                unitStats = unitStats,
                movement = movement,
                unitManager = unitManager,
                turnManager = turnManager,
                pathfinding = pathfinding,
                action = action,
                decisionDelay = decisionDelay,
                afterMoveDelay = afterMoveDelay,
                afterAttackDelay = afterAttackDelay
            };

            yield return behaviour.ExecuteTurn(context);
        }
        else
        {
            Debug.LogWarning("[AI] " + unitStats.gameObject.name + " esta sem EnemyAIBehaviour.");
            Debug.Log("[AI] " + unitStats.gameObject.name + " nao encontrou acao valida.");
        }

        Debug.Log("[AI] " + unitStats.gameObject.name + " terminou turno.");
        yield return null;
    }

    private void CancelExecution()
    {
        var action = turnAction;
        StopAllCoroutines();
        FinishExecution(action);
    }

    private void FinishExecution(TurnManager.ActionExecution action)
    {
        if (action == null || turnAction != action)
            return;

        if (movement != null)
            movement.CancelMovement();
        while (executionStack.Count > 0)
            (executionStack.Pop() as System.IDisposable)?.Dispose();
        turnAction = null;
        action.Dispose();
    }
}
