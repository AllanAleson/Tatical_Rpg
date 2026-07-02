using TMPro;
using UnityEngine;

public class WorldHPText : MonoBehaviour
{
    public UnitStats unitStats;
    public TMP_Text hpText;
    public Vector3 offset = new Vector3(0, 1.4f, 0);

    void Update()
    {
        if (unitStats == null || hpText == null)
            return;

        if (unitStats.isDowned)
        {
            hpText.text = "Desmaiado";
        }
        else
        {
            hpText.text = unitStats.currentHP + "/" + unitStats.maxHP;
        }

        transform.position = unitStats.transform.position + offset;

        transform.rotation = Camera.main.transform.rotation;
    }
}