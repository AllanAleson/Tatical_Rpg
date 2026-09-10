using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System;

public class TurnManager : MonoBehaviour
{
    public event Action OnInitiativeCreated;
    public event Action<UnitStats> OnTurnStarted;

    public UnitManager unitManager;
    public bool startCombatOnStart = true;
    public List<UnitStats> initiativeOrder = new List<UnitStats>();
    public int currentTurnIndex = -1;
    public UnitStats currentUnit;

    private bool combatStarted = false;
    private int turnVersion;
    private readonly HashSet<ActionExecution> actions = new HashSet<ActionExecution>();

    public bool IsProcessingAction => actions.Count > 0;
    public int TurnVersion => turnVersion;
    public bool CanAcceptPlayerInput => IsPlayerTurn() && !IsProcessingAction;

    // Child actions explicitly belong to an AI sequence. Old handles cannot
    // release another action/turn; cancellation keeps the lock until cleanup.
    public sealed class ActionExecution : IDisposable
    {
        internal readonly TurnManager manager;
        internal readonly UnitStats unit;
        internal readonly int version;
        internal readonly ActionExecution parent;
        internal bool cancelled;

        internal ActionExecution(TurnManager manager, UnitStats unit, ActionExecution parent)
        {
            this.manager = manager;
            this.unit = unit;
            this.parent = parent;
            version = manager.turnVersion;
        }

        public bool IsAuthorized => manager != null && manager.IsActionAuthorized(this);
        public TurnManager Manager => manager;

        public void Dispose()
        {
            cancelled = true;
            if (manager != null)
                manager.actions.Remove(this);
        }
    }

    public bool TryBeginAction(UnitStats unit, out ActionExecution action, ActionExecution parent = null)
    {
        action = null;
        if (!isActiveAndEnabled || !combatStarted || !IsCurrentUnit(unit))
            return false;

        if (parent == null)
        {
            if (IsProcessingAction)
                return false;
        }
        else
        {
            if (!IsActionAuthorized(parent) || parent.unit != unit)
                return false;

            foreach (ActionExecution running in actions)
                if (running.parent == parent)
                    return false;
        }

        action = new ActionExecution(this, unit, parent);
        actions.Add(action);
        return true;
    }

    private bool IsActionAuthorized(ActionExecution action)
    {
        return isActiveAndEnabled && action.manager == this &&
            !action.cancelled && actions.Contains(action) &&
            action.version == turnVersion && IsCurrentUnit(action.unit) &&
            (action.parent == null || IsActionAuthorized(action.parent));
    }

    public void CancelActionsForUnit(UnitStats unit)
    {
        foreach (ActionExecution action in actions)
            if (action.unit == unit)
                action.cancelled = true;
    }

    void Update()
    {
        foreach (ActionExecution action in actions)
            if (!IsActionAuthorized(action))
                action.cancelled = true;

        // Skip incapacitated units only after all action cleanup finishes.
        if (combatStarted && currentTurnIndex >= 0 && !IsProcessingAction &&
            (currentUnit == null || currentUnit.isDowned || !currentUnit.isActiveAndEnabled))
            AdvanceToNextLivingUnit();
    }

    void OnDisable()
    {
        foreach (ActionExecution action in actions)
            action.cancelled = true;

        // The manager cannot keep ownership of actions while it is disabled.
        // Their handles remain invalid and can still be disposed safely later.
        actions.Clear();
    }

    private class InitiativeEntry
    {
        public UnitStats unit;
        public int d20;
        public int tieBreaker;
    }

    void Awake()
    {
        if (unitManager == null)
            unitManager = GetComponent<UnitManager>();
    }

    void Start()
    {
        if (startCombatOnStart)
            StartCombat();
    }

    public bool IsPlayerTurn()
    {
        return combatStarted && IsCurrentUnit(currentUnit) &&
            currentUnit.team == UnitStats.Team.Player &&
            !currentUnit.isDowned;
    }

    public bool HasCombatStarted()
    {
        return combatStarted;
    }

    public bool IsCurrentUnit(UnitStats unit)
    {
        return unit != null &&
            currentUnit == unit &&
            unit.isActiveAndEnabled &&
            !currentUnit.isDowned;
    }

    public void EndPlayerTurn()
    {
        if (IsPlayerTurn())
            TryEndTurn(currentUnit);
    }

    public void StartCombat()
    {
        if (combatStarted)
        {
            Debug.Log("StartCombat ignorado: combate ja iniciado.");
            return;
        }

        if (unitManager == null)
            unitManager = UnitManager.Instance;

        if (unitManager == null)
        {
            Debug.LogWarning("TurnManager nao encontrou UnitManager para iniciar combate.");
            return;
        }

        unitManager.RefreshUnits();

        List<InitiativeEntry> entries = new List<InitiativeEntry>();
        StringBuilder rollLog = new StringBuilder();

        rollLog.AppendLine("=== INICIATIVA ===");

        foreach (UnitStats unit in unitManager.allUnits)
        {
            if (unit == null || unit.isDowned)
                continue;

            int d20 = UnityEngine.Random.Range(1, 21);
            int armorInitiativeModifier = unit.GetArmorInitiativeModifier();
            int shieldInitiativeModifier = unit.GetActiveShieldInitiativeModifier();
            unit.rolledInitiative = d20 + unit.baseInitiative + unit.InitiativeModifier;

            entries.Add(new InitiativeEntry
            {
                unit = unit,
                d20 = d20,
                tieBreaker = UnityEngine.Random.Range(int.MinValue, int.MaxValue)
            });

            rollLog.AppendLine(
                unit.gameObject.name + ": d20 " + d20 +
                " + Base Initiative " + unit.baseInitiative +
                " + Armor Initiative Modifier " + armorInitiativeModifier +
                " + Shield Initiative Modifier " + shieldInitiativeModifier +
                " = " + unit.rolledInitiative
            );
        }

        entries.Sort((a, b) =>
        {
            int rolledCompare = b.unit.rolledInitiative.CompareTo(a.unit.rolledInitiative);

            if (rolledCompare != 0)
                return rolledCompare;

            int baseCompare = b.unit.baseInitiative.CompareTo(a.unit.baseInitiative);

            if (baseCompare != 0)
                return baseCompare;

            return b.tieBreaker.CompareTo(a.tieBreaker);
        });

        initiativeOrder.Clear();

        rollLog.AppendLine("");
        rollLog.AppendLine("ORDEM:");

        for (int i = 0; i < entries.Count; i++)
        {
            UnitStats unit = entries[i].unit;
            initiativeOrder.Add(unit);
            rollLog.AppendLine((i + 1) + " - " + unit.gameObject.name + " (" + unit.rolledInitiative + ")");
        }

        Debug.Log(rollLog.ToString());

        // TODO: summons/reforcos criados depois do inicio do combate precisam de uma politica
        // propria para insercao na iniciativa. Nesta versao, a ordem atual permanece fixa.
        combatStarted = true;
        currentTurnIndex = -1;
        currentUnit = null;

        OnInitiativeCreated?.Invoke();
        AdvanceToNextLivingUnit();
    }

    public bool TryEndTurn(UnitStats unit)
    {
        if (!combatStarted || IsProcessingAction || initiativeOrder.Count == 0 ||
            !IsCurrentUnit(unit))
            return false;

        Debug.Log("Fim do turno: " + unit.gameObject.name);

        AdvanceToNextLivingUnit();
        return true;
    }

    public UnitStats GetCurrentUnit()
    {
        return currentUnit;
    }

    private void AdvanceToNextLivingUnit()
    {
        if (IsProcessingAction)
            return;

        if (initiativeOrder.Count == 0)
        {
            currentTurnIndex = -1;
            currentUnit = null;
            Debug.LogWarning("TurnManager nao tem ordem de iniciativa para avancar turno.");
            return;
        }

        for (int attempts = 0; attempts < initiativeOrder.Count; attempts++)
        {
            currentTurnIndex = (currentTurnIndex + 1) % initiativeOrder.Count;
            UnitStats candidate = initiativeOrder[currentTurnIndex];

            if (candidate == null || candidate.isDowned || !candidate.isActiveAndEnabled)
                continue;

            StartUnitTurn(candidate);
            return;
        }

        currentUnit = null;
        Debug.LogWarning("TurnManager nao encontrou nenhuma unidade viva na ordem de iniciativa.");
        currentTurnIndex = -1;
    }

    private void StartUnitTurn(UnitStats unit)
    {
        turnVersion++;
        currentUnit = unit;
        currentUnit.ResetTurnPoints();

        Debug.Log(
            "=== TURNO ===\n" +
            currentUnit.gameObject.name + "\n" +
            "Team: " + currentUnit.team
        );

        OnTurnStarted?.Invoke(currentUnit);
    }

}
