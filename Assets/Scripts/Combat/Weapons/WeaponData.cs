using UnityEngine;

public enum WeaponHandRequirement
{
    OneHanded,
    TwoHanded
}

public enum OffHandUsage
{
    Allowed,
    RequiresFreeOffHand
}

[CreateAssetMenu(menuName = "Tactical RPG/Combat/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "New Weapon";
    public WeaponHandRequirement handRequirement = WeaponHandRequirement.OneHanded;
    public OffHandUsage offHandUsage = OffHandUsage.Allowed;
    public int damageDiceCount = 1;
    public int damageDieSize = 6;
    public int actionPointCost = 3;
    public int range = 1;
    public AttributeType attackAttribute = AttributeType.Strength;

    void OnValidate()
    {
        damageDiceCount = Mathf.Max(1, damageDiceCount);
        damageDieSize = Mathf.Max(1, damageDieSize);
        actionPointCost = Mathf.Max(0, actionPointCost);
        range = Mathf.Max(1, range);
    }
}
