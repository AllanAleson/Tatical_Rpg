using System.Collections;
using UnityEngine;

public abstract class EnemyAIBehaviour : ScriptableObject
{
    public abstract IEnumerator ExecuteTurn(EnemyAITurnContext context);
}

public class EnemyAITurnContext
{
    public EnemyBrain brain;
    public UnitStats unitStats;
    public UnitMovement movement;
    public UnitManager unitManager;
    public TurnManager turnManager;
    public Pathfinding pathfinding;
    public float decisionDelay;
    public float afterMoveDelay;
    public float afterAttackDelay;
}
