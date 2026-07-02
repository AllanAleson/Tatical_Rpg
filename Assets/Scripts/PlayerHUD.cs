using TMPro;
using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    public UnitStats playerStats;
    public TMP_Text statsText;

    void Update()
    {
        if (playerStats == null || statsText == null)
            return;

        statsText.text =
            "HP: " + playerStats.currentHP + "/" + playerStats.maxHP + "\n" +
            "PA: " + playerStats.currentActionPoints + "/" + playerStats.maxActionPoints + "\n" +
            "PM: " + playerStats.currentMovePoints + "/" + playerStats.maxMovePoints;
    }
}