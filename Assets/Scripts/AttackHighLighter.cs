using System.Collections.Generic;
using UnityEngine;

public class AttackHighlighter : MonoBehaviour
{
    public GameObject attackTilePrefab;
    public PlayerMovement player;

    private List<GameObject> activeTiles = new List<GameObject>();

    public void ShowAttackRange()
    {
        ClearTiles();

        UnitStats stats = player.GetComponent<UnitStats>();

        int playerX = Mathf.RoundToInt(player.transform.position.x);
        int playerZ = Mathf.RoundToInt(player.transform.position.z);

        int range = stats.attackRange;

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);

                if (distance <= range && distance > 0)
                {
                    Vector3 position = new Vector3(
                        playerX + x,
                        0.09f,
                        playerZ + z
                    );

                    GameObject tile = Instantiate(
                        attackTilePrefab,
                        position,
                        Quaternion.identity
                    );

                    activeTiles.Add(tile);
                }
            }
        }
    }

    public void ClearTiles()
    {
        foreach (GameObject tile in activeTiles)
        {
            Destroy(tile);
        }

        activeTiles.Clear();
    }
}