using UnityEngine;

public enum ShieldCategory
{
    Light,
    Heavy
}

[CreateAssetMenu(menuName = "Tactical RPG/Combat/Shield")]
public class ShieldData : ScriptableObject
{
    public string shieldName = "New Shield";
    public ShieldCategory category = ShieldCategory.Light;
    public int defenseBonus = 0;
    public int damageReduction = 0;
    public int initiativeModifier = 0;
    public int movePointsModifier = 0;

    void OnValidate()
    {
        defenseBonus = Mathf.Max(0, defenseBonus);
        damageReduction = Mathf.Max(0, damageReduction);
    }
}
