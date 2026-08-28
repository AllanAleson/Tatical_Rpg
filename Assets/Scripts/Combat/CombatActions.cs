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

        if (distance > attacker.attackRange)
        {
            failureReason = "Alvo fora do alcance.";
            return false;
        }

        return true;
    }

    public static bool TryBasicAttack(UnitStats attacker, UnitStats target, out string failureReason)
    {
        if (!CanBasicAttack(attacker, target, out failureReason))
            return false;

        target.TakeDamage(attacker.attackDamage);
        attacker.SpendActionPoints(attacker.attackCost);
        return true;
    }

    public static int GetGridDistance(UnitStats a, UnitStats b)
    {
        if (a == null || b == null)
            return int.MaxValue;

        int ax = Mathf.RoundToInt(a.transform.position.x);
        int az = Mathf.RoundToInt(a.transform.position.z);
        int bx = Mathf.RoundToInt(b.transform.position.x);
        int bz = Mathf.RoundToInt(b.transform.position.z);

        return Mathf.Abs(bx - ax) + Mathf.Abs(bz - az);
    }
}
