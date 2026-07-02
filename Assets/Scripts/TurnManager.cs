using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public UnitStats playerStats;

    private bool playerTurn = true;

    public bool IsPlayerTurn()
    {
        return playerTurn;
    }

    public void EndPlayerTurn()
    {
        if (!playerTurn)
            return;

        Debug.Log("Fim do turno do jogador.");

        playerTurn = false;

        EnemyTurn();
    }

    private void EnemyTurn()
    {
        Debug.Log("Turno do inimigo.");

        // IA inimiga entra aqui depois

        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        Debug.Log("Novo turno do jogador.");

        playerStats.ResetTurnPoints();

        Debug.Log("PM restaurado: " + playerStats.currentMovePoints);
        Debug.Log("PA restaurado: " + playerStats.currentActionPoints);

        playerTurn = true;
    }
}