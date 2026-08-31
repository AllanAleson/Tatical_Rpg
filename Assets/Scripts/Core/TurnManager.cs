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
        return currentUnit != null &&
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
            !currentUnit.isDowned;
    }

    public void EndPlayerTurn()
    {
        EndTurn();
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

    public void EndTurn()
    {
        if (!combatStarted)
            StartCombat();

        if (!combatStarted || initiativeOrder.Count == 0)
            return;

        if (currentUnit != null)
            Debug.Log("Fim do turno: " + currentUnit.gameObject.name);

        AdvanceToNextLivingUnit();
    }

    public UnitStats GetCurrentUnit()
    {
        return currentUnit;
    }

    private void AdvanceToNextLivingUnit()
    {
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

            if (candidate == null || candidate.isDowned)
                continue;

            StartUnitTurn(candidate);
            return;
        }

        currentUnit = null;
        Debug.LogWarning("TurnManager nao encontrou nenhuma unidade viva na ordem de iniciativa.");
    }

    private void StartUnitTurn(UnitStats unit)
    {
        currentUnit = unit;
        currentUnit.ResetTurnPoints();

        Debug.Log(
            "=== TURNO ===\n" +
            currentUnit.gameObject.name + "\n" +
            "Team: " + currentUnit.team
        );

        if (currentUnit.team == UnitStats.Team.Enemy)
            Debug.Log("Turno de Enemy sem IA nesta versao. Use Fim de Turno para avancar.");

        OnTurnStarted?.Invoke(currentUnit);
    }

}
