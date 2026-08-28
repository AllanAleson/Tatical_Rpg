using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InitiativeSlot : MonoBehaviour
{
    private UnitStats representedUnit;

    public TMP_Text nameText;
    public TMP_Text initiativeText;
    public Image background;

    public Color playerColor = Color.white;
    public Color enemyColor = Color.gray;
    public Color currentColor = Color.yellow;
    public Color downedColor = new Color(1f, 1f, 1f, 0.35f);

    public void Setup(UnitStats unit, bool isCurrent)
    {
        representedUnit = unit;

        if (unit == null)
        {
            SetText("", "");
            SetBackground(downedColor);
            return;
        }

        string unitName = unit.gameObject.name;

        if (unit.isDowned)
            unitName += " (Desmaiado)";

        if (isCurrent)
            unitName = "[ ATUAL ]\n" + unitName;

        SetText(unitName, unit.rolledInitiative.ToString());
        SetBackground(GetBackgroundColor(unit, isCurrent));
    }

    public void SetCurrentTurn(bool isCurrent)
    {
        Setup(representedUnit, isCurrent);
    }

    private Color GetBackgroundColor(UnitStats unit, bool isCurrent)
    {
        if (unit.isDowned)
            return downedColor;

        if (isCurrent)
            return currentColor;

        return unit.team == UnitStats.Team.Player ? playerColor : enemyColor;
    }

    private void SetText(string unitName, string initiative)
    {
        if (nameText != null)
            nameText.text = unitName;

        if (initiativeText != null)
            initiativeText.text = initiative;
    }

    private void SetBackground(Color color)
    {
        if (background != null)
            background.color = color;
    }
}
