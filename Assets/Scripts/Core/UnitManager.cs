using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance;

    public List<UnitStats> allUnits = new List<UnitStats>();

    public UnitStats selectedUnit;

    [Header("Debug")]
    public bool logUnitRegistration = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Mais de um UnitManager ativo na cena.");
            return;
        }

        Instance = this;
    }

    void Start()
    {
        RefreshUnits();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshUnits()
    {
        HashSet<UnitStats> knownUnits = new HashSet<UnitStats>();

        for (int i = allUnits.Count - 1; i >= 0; i--)
        {
            UnitStats unit = allUnits[i];

            if (unit == null || !unit.isActiveAndEnabled || !knownUnits.Add(unit))
                allUnits.RemoveAt(i);
        }

        if (selectedUnit == null || !selectedUnit.isActiveAndEnabled || !allUnits.Contains(selectedUnit))
            selectedUnit = null;

        UnitStats[] units = FindObjectsByType<UnitStats>(
            FindObjectsInactive.Exclude
        );

        foreach (UnitStats unit in units)
        {
            RegisterUnit(unit);
        }
    }

    public void RegisterUnit(UnitStats unit)
    {
        if (unit == null || !unit.isActiveAndEnabled)
            return;

        if (allUnits.Contains(unit))
            return;

        allUnits.Add(unit);

        if (logUnitRegistration)
            Debug.Log("Unidade registrada: " + unit.gameObject.name);
    }

    public void UnregisterUnit(UnitStats unit)
    {
        if (unit == null)
            return;

        bool removed = allUnits.Remove(unit);

        if (selectedUnit == unit)
            selectedUnit = null;

        if (removed && logUnitRegistration)
            Debug.Log("Unidade removida: " + unit.gameObject.name);
    }

    public void SelectUnit(UnitStats unit)
    {
        if (unit == null)
        {
            selectedUnit = null;
            return;
        }

        if (unit.team != UnitStats.Team.Player || unit.isDowned)
            return;

        selectedUnit = unit;
    }

    public List<UnitStats> GetLivingPlayers()
    {
        return allUnits.FindAll(unit =>
            unit != null &&
            unit.team == UnitStats.Team.Player &&
            !unit.isDowned
        );
    }

    public List<UnitStats> GetLivingEnemies()
    {
        return allUnits.FindAll(unit =>
            unit != null &&
            unit.team == UnitStats.Team.Enemy &&
            !unit.isDowned
        );
    }

    public bool AreAllPlayersDown()
    {
        return GetLivingPlayers().Count == 0;
    }

    public bool AreAllEnemiesDown()
    {
        return GetLivingEnemies().Count == 0;
    }
}
