using System.Collections.Generic;
using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public enum ActionMode
    {
        None,
        Move,
        Attack,
        Skill
    }

    public PlayerMovement player;
    private PlayerMovement selectedUnit;

    public MovementHighlighter movementHighlighter;
    public Pathfinding pathfinding;
    public AttackHighlighter attackHighlighter;

    public ActionMode actionMode = ActionMode.None;

    private bool playerSelected = false;

    void Start()
    {
        selectedUnit = player;
    }

    void Update()
    {
        HandleModeInput();

        if (Input.GetMouseButtonDown(0))
        {
            if (selectedUnit != null && selectedUnit.IsMoving())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitStats clickedUnit = hit.collider.GetComponent<UnitStats>();

                if (clickedUnit != null &&
                    clickedUnit.team == UnitStats.Team.Player &&
                    !clickedUnit.isDowned)
                {
                    selectedUnit = clickedUnit.GetComponent<PlayerMovement>();

                    if (selectedUnit == null)
                    {
                        Debug.Log("Essa unidade não tem PlayerMovement.");
                        return;
                    }

                    SelectPlayer();
                    return;
                }

                if (!playerSelected || selectedUnit == null)
                    return;

                if (actionMode == ActionMode.Move)
                {
                    TryMove(hit);
                }
                else if (actionMode == ActionMode.Attack)
                {
                    TryAttack(hit);
                }
                else if (actionMode == ActionMode.Skill)
                {
                    Debug.Log("Modo skill ainda não implementado.");
                }
                else
                {
                    Debug.Log("Escolha uma ação: M mover, A atacar, S skill.");
                }
            }
        }
    }

    private void HandleModeInput()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (!playerSelected || selectedUnit == null)
            {
                Debug.Log("Selecione o player primeiro.");
                return;
            }

            actionMode = ActionMode.Move;
            attackHighlighter.ClearTiles();
            movementHighlighter.ShowMoveRange(selectedUnit.transform.position);

            Debug.Log("Modo: Movimento");
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (!playerSelected || selectedUnit == null)
            {
                Debug.Log("Selecione o player primeiro.");
                return;
            }

            actionMode = ActionMode.Attack;
            movementHighlighter.ClearTiles();
            attackHighlighter.ShowAttackRange();

            Debug.Log("Modo: Ataque");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (!playerSelected || selectedUnit == null)
            {
                Debug.Log("Selecione o player primeiro.");
                return;
            }

            actionMode = ActionMode.Skill;
            movementHighlighter.ClearTiles();
            attackHighlighter.ClearTiles();

            Debug.Log("Modo: Skill");
        }
    }

    private void SelectPlayer()
    {
        playerSelected = true;
        actionMode = ActionMode.None;

        movementHighlighter.ClearTiles();
        attackHighlighter.ClearTiles();

        Debug.Log("Player selecionado. Aperte M para mover, A para atacar, S para skill.");
    }

    private void TryMove(RaycastHit hit)
    {
        UnitStats stats = selectedUnit.GetComponent<UnitStats>();

        if (stats.currentMovePoints <= 0)
        {
            Debug.Log("Sem PM.");
            movementHighlighter.ClearTiles();
            return;
        }

        int gridX = Mathf.RoundToInt(hit.point.x);
        int gridZ = Mathf.RoundToInt(hit.point.z);

        if (pathfinding.IsBlocked(
            new Vector2Int(gridX, gridZ),
            selectedUnit.gameObject))
        {
            Debug.Log("Essa casa está bloqueada.");
            return;
        }

        List<Vector2Int> path = pathfinding.FindPath(
            selectedUnit.transform.position,
            gridX,
            gridZ
        );

        if (path != null)
        {
            selectedUnit.MoveAlongPath(path);

            movementHighlighter.ClearTiles();
            attackHighlighter.ClearTiles();

            actionMode = ActionMode.None;
        }
        else
        {
            Debug.Log("Não dá para chegar nessa casa.");
        }
    }

    private void TryAttack(RaycastHit hit)
    {
        UnitStats attackerStats = selectedUnit.GetComponent<UnitStats>();
        UnitStats targetStats = hit.collider.GetComponent<UnitStats>();

        if (targetStats == null)
        {
            Debug.Log("Alvo inválido.");
            return;
        }

        if (targetStats.isDowned)
        {
            Debug.Log("Essa unidade já está desmaiada.");
            return;
        }

        if (!attackerStats.CanAttack())
        {
            Debug.Log("Sem PA suficiente.");
            return;
        }

        int attackerX = Mathf.RoundToInt(selectedUnit.transform.position.x);
        int attackerZ = Mathf.RoundToInt(selectedUnit.transform.position.z);

        int targetX = Mathf.RoundToInt(targetStats.transform.position.x);
        int targetZ = Mathf.RoundToInt(targetStats.transform.position.z);

        int distance = Mathf.Abs(targetX - attackerX) + Mathf.Abs(targetZ - attackerZ);

        if (distance > attackerStats.attackRange)
        {
            Debug.Log("Alvo fora do alcance.");
            return;
        }

        targetStats.TakeDamage(attackerStats.attackDamage);
        attackerStats.SpendActionPoints(attackerStats.attackCost);

        Debug.Log("Ataque realizado. PA restante: " + attackerStats.currentActionPoints);

        attackHighlighter.ClearTiles();
        actionMode = ActionMode.None;
    }
}