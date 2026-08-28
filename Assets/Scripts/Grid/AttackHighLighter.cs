using System.Collections.Generic;
using UnityEngine;

public class AttackHighlighter : MonoBehaviour
{
    public GameObject attackTilePrefab;
    public ClickManager clickManager;

    private List<GameObject> activeTiles = new List<GameObject>();

    void Awake()
    {
        if (clickManager == null)
            clickManager = GetComponent<ClickManager>();
    }

    public void ShowAttackRange(PlayerMovement unit)
    {
        ClearTiles();

        if (unit == null)
        {
            Debug.LogWarning("AttackHighlighter recebeu unidade nula.");
            return;
        }

        UnitStats stats = unit.GetComponent<UnitStats>();

        if (stats == null)
        {
            Debug.LogWarning("AttackHighlighter recebeu unidade sem UnitStats: " + unit.gameObject.name);
            return;
        }

        if (stats.isDowned)
            return;

        int unitX = Mathf.RoundToInt(unit.transform.position.x);
        int unitZ = Mathf.RoundToInt(unit.transform.position.z);
        int range = stats.attackRange;

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);

                if (distance <= 0 || distance > range)
                    continue;

                if (attackTilePrefab == null)
                    continue;

                Vector3 position = new Vector3(unitX + x, 0.09f, unitZ + z);
                GameObject tile = Instantiate(attackTilePrefab, position, Quaternion.identity);

                activeTiles.Add(tile);
            }
        }
    }

    public void ShowAttackRange()
    {
        PlayerMovement selectedUnit = clickManager != null ? clickManager.SelectedUnit : null;
        ShowAttackRange(selectedUnit);
    }

    public void ClearTiles()
    {
        foreach (GameObject tile in activeTiles)
        {
            if (tile != null)
                Destroy(tile);
        }

        activeTiles.Clear();
    }
}
