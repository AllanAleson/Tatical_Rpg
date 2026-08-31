using UnityEngine;

public enum ArmorCategory
{
    Light,
    Medium,
    Heavy
}

public enum DexterityDefenseRule
{
    Full,
    Limited,
    None
}

[CreateAssetMenu(menuName = "Tactical RPG/Combat/Armor")]
public class ArmorData : ScriptableObject
{
    public string armorName = "New Armor";
    public ArmorCategory category = ArmorCategory.Light;
    public int defenseBonus = 0;
    public int damageReduction = 0;
    public DexterityDefenseRule dexterityDefenseRule = DexterityDefenseRule.Full;
    public int maxDexterityBonus = 0;
    public int initiativeModifier = 0;
    public int movePointsModifier = 0;

    void OnValidate()
    {
        defenseBonus = Mathf.Max(0, defenseBonus);
        damageReduction = Mathf.Max(0, damageReduction);
        maxDexterityBonus = Mathf.Max(0, maxDexterityBonus);
    }
}
