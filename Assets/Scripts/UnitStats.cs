using UnityEngine;

public class UnitStats : MonoBehaviour
{
    public enum Team
    {
        Player,
        Enemy
    }

    [Header("Equipe")]
    public Team team;

    [Header("Estado")]
    public bool isDowned = false;

    [Header("Vida")]
    public int maxHP = 10;
    public int currentHP = 10;

    [Header("Movimento")]
    public int maxMovePoints = 4;
    public int currentMovePoints = 4;

    [Header("Ação")]
    public int maxActionPoints = 6;
    public int currentActionPoints = 6;

    [Header("Ataque Básico")]
    public int attackDamage = 3;
    public int attackRange = 1;
    public int attackCost = 2;

    public void ResetTurnPoints()
    {
        if (isDowned)
            return;

        currentMovePoints = maxMovePoints;
        currentActionPoints = maxActionPoints;
    }

    public void SpendMovePoints(int amount)
    {
        currentMovePoints -= amount;

        if (currentMovePoints < 0)
            currentMovePoints = 0;
    }

    public bool CanAttack()
    {
        return !isDowned && currentActionPoints >= attackCost;
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