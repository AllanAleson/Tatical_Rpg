using UnityEngine;

public enum AttributeType
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma
}

public class UnitStats : MonoBehaviour
{
    public const int BaseDefense = 10;

    public enum Team
    {
        Player,
        Enemy
    }

    [Header("Equipe")]
    public Team team;

    [Header("Estado")]
    public bool isDowned = false;

    [Header("Iniciativa")]
    public int baseInitiative = 0;
    public int rolledInitiative = 0;

    [Header("Atributos")]
    public int strength = 10;
    public int dexterity = 10;
    public int constitution = 10;
    public int intelligence = 10;
    public int wisdom = 10;
    public int charisma = 10;

    [Header("Proficiencia")]
    public int proficiencyBonus = 0;

    [Header("Vida")]
    public int maxHP = 10;
    public int currentHP = 10;

    [Header("Movimento")]
    public int maxMovePoints = 4;
    public int currentMovePoints = 4;

    [Header("Ação")]
    public int maxActionPoints = 6;
    public int currentActionPoints = 6;

    [Header("Arma")]
    public WeaponData equippedWeapon;

    [Header("Armadura")]
    public ArmorData equippedArmor;

    [Header("Escudo")]
    public ShieldData equippedShield;

    [Header("Ataque Desarmado")]
    public int unarmedDamageDiceCount = 1;
    public int unarmedDamageDieSize = 4;
    public int unarmedAttackCost = 2;
    public int unarmedAttackRange = 1;
    public AttributeType unarmedAttackAttribute = AttributeType.Strength;

    public int Defense
    {
        get
        {
            return BaseDefense +
                GetCurrentDexterityDefenseModifier() +
                GetCurrentArmorDefenseBonus() +
                GetCurrentShieldDefenseBonus();
        }
    }

    public int DamageReduction
    {
        get
        {
            int damageReduction = 0;

            if (equippedArmor != null)
                damageReduction += equippedArmor.damageReduction;

            if (IsEquippedShieldActive)
                damageReduction += equippedShield.damageReduction;

            return damageReduction;
        }
    }

    public int InitiativeModifier
    {
        get
        {
            int initiativeModifier = 0;

            initiativeModifier += GetArmorInitiativeModifier();
            initiativeModifier += GetActiveShieldInitiativeModifier();

            return initiativeModifier;
        }
    }

    public int MovePointsModifier
    {
        get
        {
            int movePointsModifier = 0;

            movePointsModifier += GetArmorMovePointsModifier();
            movePointsModifier += GetActiveShieldMovePointsModifier();

            return movePointsModifier;
        }
    }

    public int EffectiveMaxMovePoints
    {
        get { return Mathf.Max(1, maxMovePoints + MovePointsModifier); }
    }

    public bool IsEquippedShieldActive
    {
        get { return equippedShield != null && CanUseShieldWithCurrentWeapon(); }
    }

    void OnEnable()
    {
        UnitManager.Instance?.RegisterUnit(this);
    }

    void Start()
    {
        UnitManager.Instance?.RegisterUnit(this);
    }

    void OnDisable()
    {
        UnitManager.Instance?.UnregisterUnit(this);
    }

    void OnValidate()
    {
        unarmedDamageDiceCount = Mathf.Max(1, unarmedDamageDiceCount);
        unarmedDamageDieSize = Mathf.Max(1, unarmedDamageDieSize);
        unarmedAttackCost = Mathf.Max(0, unarmedAttackCost);
        unarmedAttackRange = Mathf.Max(1, unarmedAttackRange);
    }

    public void ResetTurnPoints()
    {
        if (isDowned)
            return;

        currentMovePoints = EffectiveMaxMovePoints;
        currentActionPoints = maxActionPoints;
    }

    public int GetAttributeValue(AttributeType attribute)
    {
        switch (attribute)
        {
            case AttributeType.Strength:
                return strength;
            case AttributeType.Dexterity:
                return dexterity;
            case AttributeType.Constitution:
                return constitution;
            case AttributeType.Intelligence:
                return intelligence;
            case AttributeType.Wisdom:
                return wisdom;
            case AttributeType.Charisma:
                return charisma;
            default:
                Debug.LogWarning("Atributo desconhecido: " + attribute);
                return 10;
        }
    }

    public int GetAttributeModifier(AttributeType attribute)
    {
        return CalculateAttributeModifier(GetAttributeValue(attribute));
    }

    public static int CalculateAttributeModifier(int attributeValue)
    {
        return Mathf.FloorToInt((attributeValue - 10) / 2f);
    }

    public int GetAttackBonus(AttributeType attribute)
    {
        return GetAttributeModifier(attribute) + proficiencyBonus;
    }

    public int GetCurrentDexterityDefenseModifier()
    {
        int dexterityModifier = GetAttributeModifier(AttributeType.Dexterity);

        if (equippedArmor == null)
            return dexterityModifier;

        switch (equippedArmor.dexterityDefenseRule)
        {
            case DexterityDefenseRule.Full:
                return dexterityModifier;
            case DexterityDefenseRule.Limited:
                if (dexterityModifier > 0)
                    return Mathf.Min(dexterityModifier, equippedArmor.maxDexterityBonus);

                return dexterityModifier;
            case DexterityDefenseRule.None:
                return Mathf.Min(dexterityModifier, 0);
            default:
                Debug.LogWarning("Regra de Destreza na Defesa desconhecida: " + equippedArmor.dexterityDefenseRule);
                return dexterityModifier;
        }
    }

    public int GetCurrentArmorDefenseBonus()
    {
        if (equippedArmor != null)
            return equippedArmor.defenseBonus;

        return 0;
    }

    public int GetCurrentShieldDefenseBonus()
    {
        if (IsEquippedShieldActive)
            return equippedShield.defenseBonus;

        return 0;
    }

    public int GetArmorInitiativeModifier()
    {
        if (equippedArmor != null)
            return equippedArmor.initiativeModifier;

        return 0;
    }

    public int GetActiveShieldInitiativeModifier()
    {
        if (IsEquippedShieldActive)
            return equippedShield.initiativeModifier;

        return 0;
    }

    public int GetArmorMovePointsModifier()
    {
        if (equippedArmor != null)
            return equippedArmor.movePointsModifier;

        return 0;
    }

    public int GetActiveShieldMovePointsModifier()
    {
        if (IsEquippedShieldActive)
            return equippedShield.movePointsModifier;

        return 0;
    }

    public bool CanUseShieldWithCurrentWeapon()
    {
        if (equippedWeapon == null)
            return true;

        if (equippedWeapon.handRequirement == WeaponHandRequirement.TwoHanded)
            return false;

        return equippedWeapon.offHandUsage == OffHandUsage.Allowed;
    }

    public AttributeType GetCurrentAttackAttribute()
    {
        if (equippedWeapon != null)
            return equippedWeapon.attackAttribute;

        return unarmedAttackAttribute;
    }

    public int GetCurrentAttackCost()
    {
        if (equippedWeapon != null)
            return equippedWeapon.actionPointCost;

        return unarmedAttackCost;
    }

    public int GetCurrentAttackRange()
    {
        if (equippedWeapon != null)
            return equippedWeapon.range;

        return unarmedAttackRange;
    }

    public int GetCurrentDamageDiceCount()
    {
        if (equippedWeapon != null)
            return equippedWeapon.damageDiceCount;

        return unarmedDamageDiceCount;
    }

    public int GetCurrentDamageDieSize()
    {
        if (equippedWeapon != null)
            return equippedWeapon.damageDieSize;

        return unarmedDamageDieSize;
    }

    public string GetCurrentWeaponName()
    {
        if (equippedWeapon != null && !string.IsNullOrWhiteSpace(equippedWeapon.weaponName))
            return equippedWeapon.weaponName;

        return "Ataque Desarmado";
    }

    public bool IsUsingWeaponData()
    {
        return equippedWeapon != null;
    }

    public void SpendMovePoints(int amount)
    {
        currentMovePoints -= amount;

        if (currentMovePoints < 0)
            currentMovePoints = 0;
    }

    public bool CanAttack()
    {
        return !isDowned && currentActionPoints >= GetCurrentAttackCost();
    }

    public void SpendActionPoints(int amount)
    {
        currentActionPoints -= amount;

        if (currentActionPoints < 0)
            currentActionPoints = 0;
    }

    public void TakeDamage(int amount)
    {
        if (isDowned)
            return;

        currentHP -= amount;

        if (currentHP <= 0)
        {
            currentHP = 0;
            DownUnit();
        }

        Debug.Log(gameObject.name + " HP: " + currentHP + "/" + maxHP);
    }

    private void DownUnit()
    {
        isDowned = true;
        currentMovePoints = 0;
        currentActionPoints = 0;

        Debug.Log(gameObject.name + " ficou desmaiado.");
    }

    public void Revive(int hpAmount)
    {
        isDowned = false;
        currentHP = Mathf.Clamp(hpAmount, 1, maxHP);
        ResetTurnPoints();

        Debug.Log(gameObject.name + " foi revivido.");
    }
}
