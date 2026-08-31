using System.Collections.Generic;
using UnityEngine;

public static class CombatActions
{
    public static bool CanBasicAttack(UnitStats attacker, UnitStats target, out string failureReason)
    {
        failureReason = "";

        if (attacker == null)
        {
            failureReason = "Atacante invalido.";
            return false;
        }

        if (target == null)
        {
            failureReason = "Alvo invalido.";
            return false;
        }

        if (attacker.isDowned)
        {
            failureReason = "Atacante esta desmaiado.";
            return false;
        }

        if (target.isDowned)
        {
            failureReason = "Essa unidade ja esta desmaiada.";
            return false;
        }

        if (!attacker.CanAttack())
        {
            failureReason = "Sem PA suficiente.";
            return false;
        }

        int distance = GetGridDistance(attacker, target);

        if (distance > attacker.GetCurrentAttackRange())
        {
            failureReason = "Alvo fora do alcance.";
            return false;
        }

        if (!HasClearAttackLine(attacker, target))
        {
            failureReason = "Linha de ataque bloqueada.";
            return false;
        }

        return true;
    }

    public static bool CanAttackCell(UnitStats attacker, Vector2Int targetCell, out string failureReason)
    {
        failureReason = "";

        if (attacker == null)
        {
            failureReason = "Atacante invalido.";
            return false;
        }

        if (attacker.isDowned)
        {
            failureReason = "Atacante esta desmaiado.";
            return false;
        }

        Vector2Int attackerCell = GetUnitCell(attacker);
        int distance = GetGridDistance(attackerCell, targetCell);

        if (distance <= 0 || distance > attacker.GetCurrentAttackRange())
        {
            failureReason = "Alvo fora do alcance.";
            return false;
        }

        if (!HasClearAttackLine(attackerCell, targetCell, attacker.gameObject, null))
        {
            failureReason = "Linha de ataque bloqueada.";
            return false;
        }

        return true;
    }

    public static bool CanAttackFromCell(UnitStats attacker, Vector2Int attackerCell, UnitStats target, out string failureReason)
    {
        failureReason = "";

        if (attacker == null)
        {
            failureReason = "Atacante invalido.";
            return false;
        }

        if (target == null)
        {
            failureReason = "Alvo invalido.";
            return false;
        }

        if (attacker.isDowned)
        {
            failureReason = "Atacante esta desmaiado.";
            return false;
        }

        if (target.isDowned)
        {
            failureReason = "Essa unidade ja esta desmaiada.";
            return false;
        }

        Vector2Int targetCell = GetUnitCell(target);
        int distance = GetGridDistance(attackerCell, targetCell);

        if (distance <= 0 || distance > attacker.GetCurrentAttackRange())
        {
            failureReason = "Alvo fora do alcance.";
            return false;
        }

        if (!HasClearAttackLine(attackerCell, targetCell, attacker.gameObject, target.gameObject))
        {
            failureReason = "Linha de ataque bloqueada.";
            return false;
        }

        return true;
    }

    public static bool HasClearAttackLine(UnitStats attacker, UnitStats target)
    {
        if (attacker == null || target == null)
            return false;

        return HasClearAttackLine(
            GetUnitCell(attacker),
            GetUnitCell(target),
            attacker.gameObject,
            target.gameObject
        );
    }

    public static bool HasClearAttackLine(
        Vector2Int origin,
        Vector2Int target,
        GameObject ignoredAttacker,
        GameObject ignoredTarget)
    {
        if (origin == target)
            return true;

        List<Vector2Int> checkedCells = GetAttackLineBlockingCells(origin, target);

        foreach (Vector2Int cell in checkedCells)
        {
            if (IsAttackLineBlocked(cell, ignoredAttacker, ignoredTarget))
                return false;
        }

        return true;
    }

    public static bool TryBasicAttack(UnitStats attacker, UnitStats target, out string failureReason)
    {
        if (!CanBasicAttack(attacker, target, out failureReason))
            return false;

        AttributeType attackAttribute = attacker.GetCurrentAttackAttribute();
        int actionPointCost = attacker.GetCurrentAttackCost();
        int d20 = Random.Range(1, 21);
        int attackBonus = attacker.GetAttackBonus(attackAttribute);
        int attackTotal = d20 + attackBonus;
        int defense = target.Defense;

        bool isNaturalOne = d20 == 1;
        bool isNaturalTwenty = d20 == 20;
        bool isHit = isNaturalTwenty || (!isNaturalOne && attackTotal > defense);

        attacker.SpendActionPoints(actionPointCost);

        int damageBeforeReduction = 0;
        int damageReduction = 0;
        int finalDamage = 0;
        int damageRollTotal = 0;
        int criticalBaseDiceMaximum = 0;
        int damageAttributeModifier = 0;
        string damageRoll = "";

        if (isHit)
        {
            damageBeforeReduction = RollBasicAttackDamage(
                attacker,
                attackAttribute,
                isNaturalTwenty,
                out damageRoll,
                out damageRollTotal,
                out criticalBaseDiceMaximum,
                out damageAttributeModifier
            );

            damageReduction = target.DamageReduction;
            finalDamage = Mathf.Max(0, damageBeforeReduction - damageReduction);
        }

        LogBasicAttackResolution(
            attacker,
            target,
            attacker.GetCurrentWeaponName(),
            attackAttribute,
            d20,
            attackBonus,
            attackTotal,
            defense,
            isNaturalOne,
            isNaturalTwenty,
            isHit,
            damageBeforeReduction,
            damageReduction,
            finalDamage,
            damageRollTotal,
            criticalBaseDiceMaximum,
            damageAttributeModifier,
            damageRoll
        );

        if (isHit && finalDamage > 0)
            target.TakeDamage(finalDamage);

        return true;
    }

    private static int RollBasicAttackDamage(
        UnitStats attacker,
        AttributeType attackAttribute,
        bool isCriticalHit,
        out string damageRoll,
        out int damageRollTotal,
        out int criticalBaseDiceMaximum,
        out int damageAttributeModifier)
    {
        damageRollTotal = 0;
        int damageDiceCount = attacker.GetCurrentDamageDiceCount();
        int damageDieSize = attacker.GetCurrentDamageDieSize();

        for (int i = 0; i < damageDiceCount; i++)
        {
            damageRollTotal += Random.Range(1, damageDieSize + 1);
        }

        damageAttributeModifier = attacker.GetAttributeModifier(attackAttribute);
        criticalBaseDiceMaximum = isCriticalHit ? damageDiceCount * damageDieSize : 0;
        damageRoll = damageDiceCount + "d" + damageDieSize;

        return Mathf.Max(1, criticalBaseDiceMaximum + damageRollTotal + damageAttributeModifier);
    }

    private static void LogBasicAttackResolution(
        UnitStats attacker,
        UnitStats target,
        string weaponName,
        AttributeType attackAttribute,
        int d20,
        int attackBonus,
        int attackTotal,
        int defense,
        bool isNaturalOne,
        bool isNaturalTwenty,
        bool isHit,
        int damageBeforeReduction,
        int damageReduction,
        int finalDamage,
        int damageRollTotal,
        int criticalBaseDiceMaximum,
        int damageAttributeModifier,
        string damageRoll)
    {
        string specialRoll = "";

        if (isNaturalOne)
            specialRoll = " (natural 1: MISS automatico)";
        else if (isNaturalTwenty)
            specialRoll = " (natural 20: CRITICAL HIT automatico)";

        string result = "MISS";

        if (isHit)
            result = isNaturalTwenty ? "CRITICAL HIT" : "HIT";

        Debug.Log(
            attacker.gameObject.name + " atacou " + target.gameObject.name +
            " com " + weaponName + "\n" +
            "Atributo: " + attackAttribute + "\n" +
            "d20: " + d20 + specialRoll + "\n" +
            "Attack Bonus: " + FormatSigned(attackBonus) + "\n" +
            "Total: " + attackTotal + "\n" +
            "Defense: " + defense + "\n" +
            "Resultado: " + result + "\n" +
            "Critical Base Dice Maximum: " + (isNaturalTwenty ? criticalBaseDiceMaximum.ToString() : "nenhum") + "\n" +
            "Damage Roll: " + (isHit ? damageRollTotal + " (" + damageRoll + ")" : "nenhum") + "\n" +
            "Damage Attribute Modifier: " + (isHit ? FormatSigned(damageAttributeModifier) : "nenhum") + "\n" +
            "Damage Before Reduction: " + (isHit ? damageBeforeReduction.ToString() : "nenhum") + "\n" +
            "Damage Reduction: " + (isHit ? damageReduction.ToString() : "nenhum") + "\n" +
            "Damage Final: " + (isHit ? finalDamage.ToString() : "nenhum") + "\n" +
            "PA restante: " + attacker.currentActionPoints
        );
    }

    private static string FormatSigned(int value)
    {
        if (value >= 0)
            return "+" + value;

        return value.ToString();
    }

    public static int GetGridDistance(UnitStats a, UnitStats b)
    {
        if (a == null || b == null)
            return int.MaxValue;

        return GetGridDistance(GetUnitCell(a), GetUnitCell(b));
    }

    public static int GetGridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(b.x - a.x) + Mathf.Abs(b.y - a.y);
    }

    private static Vector2Int GetUnitCell(UnitStats unit)
    {
        return new Vector2Int(
            Mathf.RoundToInt(unit.transform.position.x),
            Mathf.RoundToInt(unit.transform.position.z)
        );
    }

    private static List<Vector2Int> GetAttackLineBlockingCells(Vector2Int origin, Vector2Int target)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int deltaX = target.x - origin.x;
        int deltaY = target.y - origin.y;
        int stepCount = Mathf.Max(Mathf.Abs(deltaX), Mathf.Abs(deltaY));

        if (stepCount <= 0)
            return cells;

        Vector2Int previous = origin;

        for (int i = 1; i <= stepCount; i++)
        {
            float t = i / (float)stepCount;
            Vector2Int current = new Vector2Int(
                Mathf.RoundToInt(origin.x + deltaX * t),
                Mathf.RoundToInt(origin.y + deltaY * t)
            );

            AddCornerCellsIfNeeded(cells, previous, current);

            if (current != origin && current != target && !cells.Contains(current))
                cells.Add(current);

            previous = current;
        }

        return cells;
    }

    private static void AddCornerCellsIfNeeded(List<Vector2Int> cells, Vector2Int previous, Vector2Int current)
    {
        int deltaX = current.x - previous.x;
        int deltaY = current.y - previous.y;

        if (Mathf.Abs(deltaX) != 1 || Mathf.Abs(deltaY) != 1)
            return;

        Vector2Int horizontalCell = previous + new Vector2Int(deltaX, 0);
        Vector2Int verticalCell = previous + new Vector2Int(0, deltaY);

        if (!cells.Contains(horizontalCell))
            cells.Add(horizontalCell);

        if (!cells.Contains(verticalCell))
            cells.Add(verticalCell);
    }

    private static bool IsAttackLineBlocked(
        Vector2Int cell,
        GameObject ignoredAttacker,
        GameObject ignoredTarget)
    {
        Vector3 checkPosition = new Vector3(cell.x, 0.5f, cell.y);

        Collider[] hits = Physics.OverlapBox(
            checkPosition,
            new Vector3(0.4f, 0.4f, 0.4f)
        );

        foreach (Collider hit in hits)
        {
            if (ShouldIgnoreLineBlocker(hit, ignoredAttacker) ||
                ShouldIgnoreLineBlocker(hit, ignoredTarget))
            {
                continue;
            }

            UnitStats stats = hit.GetComponentInParent<UnitStats>();

            if (stats != null)
            {
                if (stats.isDowned)
                    continue;

                return true;
            }

            if (hit.CompareTag("Obstacle"))
                return true;
        }

        return false;
    }

    private static bool ShouldIgnoreLineBlocker(Collider hit, GameObject ignoredObject)
    {
        if (hit == null || ignoredObject == null)
            return false;

        return hit.gameObject == ignoredObject || hit.transform.IsChildOf(ignoredObject.transform);
    }
}
