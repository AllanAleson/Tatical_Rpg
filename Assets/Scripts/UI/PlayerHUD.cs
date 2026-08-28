using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public TMP_Text statsText;
    public ClickManager clickManager;
    public UnitManager unitManager;

    void Awake()
    {
        if (clickManager == null)
            clickManager = GetComponent<ClickManager>();

        if (unitManager == null)
            unitManager = GetComponent<UnitManager>();
    }

    void Update()
    {
        if (statsText == null)
            return;

        UnitStats selectedStats = GetSelectedStats();

        if (selectedStats == null)
        {
            statsText.text = "";
            return;
        }

        statsText.text =
            "HP: " + selectedStats.currentHP + "/" + selectedStats.maxHP + "\n" +
            "PA: " + selectedStats.currentActionPoints + "/" + selectedStats.maxActionPoints + "\n" +
            "PM: " + selectedStats.currentMovePoints + "/" + selectedStats.maxMovePoints;
    }

    private UnitStats GetSelectedStats()
    {
        UnitStats selectedStats = clickManager != null ? clickManager.SelectedUnitStats : null;

        if (IsValidPlayerSelection(selectedStats))
            return selectedStats;

        selectedStats = unitManager != null ? unitManager.selectedUnit : null;

        if (IsValidPlayerSelection(selectedStats))
            return selectedStats;

        return null;
    }

    private bool IsValidPlayerSelection(UnitStats stats)
    {
        return stats != null &&
            stats.team == UnitStats.Team.Player &&
            !stats.isDowned;
    }
}
