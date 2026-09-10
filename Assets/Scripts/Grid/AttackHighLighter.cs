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

        GridManager grid = GridManager.Instance != null
            ? GridManager.Instance
            : FindAnyObjectByType<GridManager>();
        if (grid == null)
            return;

        Vector2Int unitCell = grid.WorldToCell(unit.transform.position);
        int range = stats.GetCurrentAttackRange();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                int distance = Mathf.Abs(x) + Mathf.Abs(z);

                if (distance <= 0 || distance > range)
                    continue;

                Vector2Int targetCell = unitCell + new Vector2Int(x, z);
                string failureReason;

                if (!grid.IsCellValid(targetCell) ||
                    !CombatActions.CanAttackCell(stats, targetCell, out failureReason))
                    continue;

                if (attackTilePrefab == null)
                    continue;

                Vector3 position = grid.CellToWorld(targetCell, 0.09f);
                GameObject tile = Instantiate(attackTilePrefab, position, Quaternion.identity);
                grid.ValidateCellVisual(tile, targetCell, "Attack highlight");

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
