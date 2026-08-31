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

    public MovementHighlighter movementHighlighter;
    public Pathfinding pathfinding;
    public AttackHighlighter attackHighlighter;

    public UnitManager unitManager;
    public TurnManager turnManager;

    public ActionMode actionMode = ActionMode.None;

    private PlayerMovement selectedUnit;

    public PlayerMovement SelectedUnit
    {
        get { return selectedUnit; }
    }

    public UnitStats SelectedUnitStats
    {
        get
        {
            if (selectedUnit == null)
                return null;

            return selectedUnit.GetComponent<UnitStats>();
        }
    }

    void Awake()
    {
        if (unitManager == null)
            unitManager = GetComponent<UnitManager>();

        if (turnManager == null)
            turnManager = GetComponent<TurnManager>();
    }

    void Update()
    {
        SyncSelectionWithTurn();
        HandleModeInput();

        if (!Input.GetMouseButtonDown(0))
            return;

        if (selectedUnit != null && selectedUnit.IsMoving())
            return;

        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("ClickManager nao encontrou Camera.main.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        UnitStats clickedUnit = hit.collider.GetComponentInParent<UnitStats>();

        if (TrySelectPlayerUnit(clickedUnit))
            return;

        if (selectedUnit == null)
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
            Debug.Log("Modo skill ainda nao implementado.");
        }
        else
        {
            Debug.Log("Escolha uma acao: M mover, A atacar, S skill.");
        }
    }

    private bool TrySelectPlayerUnit(UnitStats clickedUnit)
    {
        if (clickedUnit == null)
            return false;

        if (clickedUnit.team != UnitStats.Team.Player || clickedUnit.isDowned)
            return false;

        if (!IsUnitAllowedThisTurn(clickedUnit))
        {
            Debug.Log("Essa unidade Player nao esta com o turno ativo.");
            return true;
        }

        PlayerMovement movement = clickedUnit.GetComponent<PlayerMovement>();

        if (movement == null)
        {
            Debug.LogWarning(
                "Unidade Player clicada nao tem PlayerMovement: " +
                clickedUnit.gameObject.name
            );

            return true;
        }

        selectedUnit = movement;

        if (unitManager != null)
            unitManager.SelectUnit(clickedUnit);

        SelectPlayer();

        return true;
    }

    private void HandleModeInput()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            UnitStats stats = SelectedUnitStats;

            if (!CanUseSelectedUnit(stats))
                return;

            actionMode = ActionMode.Move;
            ClearAttackTiles();
            ShowMoveTiles(selectedUnit, stats);

            Debug.Log("Modo: Movimento");
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            UnitStats stats = SelectedUnitStats;

            if (!CanUseSelectedUnit(stats))
                return;

            actionMode = ActionMode.Attack;
            ClearMoveTiles();
            ShowAttackTiles(selectedUnit);

            Debug.Log("Modo: Ataque");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            UnitStats stats = SelectedUnitStats;

            if (!CanUseSelectedUnit(stats))
                return;

            actionMode = ActionMode.Skill;
            ClearMoveTiles();
            ClearAttackTiles();

            Debug.Log("Modo: Skill");
        }
    }

    private bool CanUseSelectedUnit(UnitStats stats)
    {
        if (selectedUnit == null || stats == null)
        {
            Debug.Log("Selecione uma unidade primeiro.");
            return false;
        }

        if (stats.isDowned)
        {
            Debug.Log("Unidade selecionada esta desmaiada.");
            return false;
        }

        if (!IsUnitAllowedThisTurn(stats))
        {
            Debug.Log("A unidade selecionada nao esta com o turno ativo.");
            return false;
        }

        return true;
    }

    private bool IsUnitAllowedThisTurn(UnitStats stats)
    {
        if (turnManager == null)
            return true;

        if (!turnManager.HasCombatStarted())
            return true;

        return turnManager.IsCurrentUnit(stats) &&
            stats.team == UnitStats.Team.Player;
    }

    private void SyncSelectionWithTurn()
    {
        if (turnManager == null || !turnManager.HasCombatStarted())
            return;

        UnitStats stats = SelectedUnitStats;

        if (stats == null || IsUnitAllowedThisTurn(stats))
            return;

        if (actionMode == ActionMode.None)
            return;

        actionMode = ActionMode.None;
        ClearMoveTiles();
        ClearAttackTiles();
    }

    private void SelectPlayer()
    {
        actionMode = ActionMode.None;

        ClearMoveTiles();
        ClearAttackTiles();

        Debug.Log("Unidade selecionada. Aperte M para mover, A para atacar, S para skill.");
    }

    private void TryMove(RaycastHit hit)
    {
        UnitStats stats = SelectedUnitStats;

        if (!CanUseSelectedUnit(stats))
            return;

        if (pathfinding == null)
        {
            Debug.LogWarning("ClickManager esta sem referencia de Pathfinding.");
            return;
        }

        if (stats.currentMovePoints <= 0)
        {
            Debug.Log("Sem PM.");
            ClearMoveTiles();
            return;
        }

        int gridX = Mathf.RoundToInt(hit.point.x);
        int gridZ = Mathf.RoundToInt(hit.point.z);

        Vector2Int startCell = new Vector2Int(
            Mathf.RoundToInt(selectedUnit.transform.position.x),
            Mathf.RoundToInt(selectedUnit.transform.position.z)
        );
        Vector2Int targetCell = new Vector2Int(gridX, gridZ);

        if (targetCell == startCell)
        {
            Debug.Log("A unidade ja esta nessa casa.");
            return;
        }

        if (pathfinding.IsBlocked(targetCell, selectedUnit.gameObject))
        {
            Debug.Log("Essa casa esta bloqueada.");
            return;
        }

        PathResult path = pathfinding.GetReachablePath(
            selectedUnit.transform.position,
            targetCell,
            stats
        );

        if (path != null && path.HasSteps)
        {
            selectedUnit.MoveAlongPath(path);

            ClearMoveTiles();
            ClearAttackTiles();

            actionMode = ActionMode.None;
        }
        else
        {
            Debug.Log("Nao da para chegar nessa casa.");
        }
    }

    private void TryAttack(RaycastHit hit)
    {
        UnitStats attackerStats = SelectedUnitStats;
        UnitStats targetStats = hit.collider.GetComponentInParent<UnitStats>();

        if (!CanUseSelectedUnit(attackerStats))
            return;

        if (targetStats == null)
        {
            Debug.Log("Alvo invalido.");
            return;
        }

        string failureReason;

        if (!CombatActions.TryBasicAttack(attackerStats, targetStats, out failureReason))
        {
            Debug.Log(failureReason);
            return;
        }

        Debug.Log("Ataque realizado. PA restante: " + attackerStats.currentActionPoints);

        ClearAttackTiles();
        actionMode = ActionMode.None;
    }

    private void ShowMoveTiles(PlayerMovement unit, UnitStats stats)
    {
        if (movementHighlighter == null)
        {
            Debug.LogWarning("ClickManager esta sem referencia de MovementHighlighter.");
            return;
        }

        movementHighlighter.ShowMoveRange(unit, stats);
    }

    private void ShowAttackTiles(PlayerMovement unit)
    {
        if (attackHighlighter == null)
        {
            Debug.LogWarning("ClickManager esta sem referencia de AttackHighlighter.");
            return;
        }

        attackHighlighter.ShowAttackRange(unit);
    }

    private void ClearMoveTiles()
    {
        if (movementHighlighter != null)
            movementHighlighter.ClearTiles();
    }

    private void ClearAttackTiles()
    {
        if (attackHighlighter != null)
            attackHighlighter.ClearTiles();
    }
}
