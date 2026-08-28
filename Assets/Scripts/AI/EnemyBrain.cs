using System.Collections;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public UnitStats unitStats;
    public UnitMovement movement;
    public EnemyAIBehaviour behaviour;
    public TurnManager turnManager;
    public UnitManager unitManager;
    public Pathfinding pathfinding;

    public float decisionDelay = 0.35f;
    public float afterMoveDelay = 0.35f;
    public float afterAttackDelay = 0.35f;

    private bool isExecutingTurn = false;

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
    }

    private void HandleTurnStarted(UnitStats turnUnit)
    {
        if (turnUnit == null || turnUnit != unitStats)
            return;

        if (unitStats == null || unitStats.team != UnitStats.Team.Enemy || unitStats.isDowned)
            return;

        if (isExecutingTurn)
            return;

        StartCoroutine(ExecuteEnemyTurn());
    }

    private IEnumerator ExecuteEnemyTurn()
    {
        isExecutingTurn = true;
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
        isExecutingTurn = false;

        yield return null;

        if (turnManager != null && turnManager.IsCurrentUnit(unitStats))
            turnManager.EndTurn();
    }
}
