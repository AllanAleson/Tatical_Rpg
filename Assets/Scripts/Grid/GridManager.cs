using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;

    [Header("Floor Tiles")]
    public GameObject legacyFloorSource;
    public float floorTileThickness = 0.1f;
    public int floorLayer = 6;
    public float validationTolerance = 0.01f;

    private readonly Dictionary<Vector2Int, GridFloorTile> floorTiles =
        new Dictionary<Vector2Int, GridFloorTile>();

    public int MinCellX => -width / 2;
    public int MinCellZ => -height / 2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Mais de um GridManager ativo na cena.");
            return;
        }

        Instance = this;
        RegisterSceneFloorTiles();
        ValidateRegisteredTiles();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        cellSize = Mathf.Max(0.01f, cellSize);
        floorTileThickness = Mathf.Max(0.01f, floorTileThickness);
        floorLayer = Mathf.Clamp(floorLayer, 0, 31);
        validationTolerance = Mathf.Max(0.0001f, validationTolerance);
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt((worldPosition.x - transform.position.x) / cellSize),
            Mathf.RoundToInt((worldPosition.z - transform.position.z) / cellSize)
        );
    }

    public Vector3 CellToWorld(Vector2Int cell, float y)
    {
        return new Vector3(
            transform.position.x + cell.x * cellSize,
            y,
            transform.position.z + cell.y * cellSize
        );
    }

    public bool IsWithinBounds(Vector2Int cell)
    {
        return cell.x >= MinCellX && cell.x < MinCellX + width &&
            cell.y >= MinCellZ && cell.y < MinCellZ + height;
    }

    public bool HasFloor(Vector2Int cell)
    {
        return floorTiles.TryGetValue(cell, out GridFloorTile tile) &&
            tile != null && tile.isActiveAndEnabled;
    }

    public bool IsCellValid(Vector2Int cell)
    {
        return IsWithinBounds(cell) && HasFloor(cell);
    }

    public bool IsCellBlocked(Vector2Int cell, GameObject ignoredObject = null)
    {
        Vector3 checkPosition = CellToWorld(cell, 0.5f);
        Vector3 halfExtents = new Vector3(cellSize * 0.4f, 0.4f, cellSize * 0.4f);
        Collider[] hits = Physics.OverlapBox(
            checkPosition,
            halfExtents,
            Quaternion.identity,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (ignoredObject != null &&
                (hit.gameObject == ignoredObject || hit.transform.IsChildOf(ignoredObject.transform)))
                continue;

            UnitStats stats = hit.GetComponentInParent<UnitStats>();
            if (stats != null)
            {
                if (!stats.isDowned)
                    return true;

                continue;
            }

            if (hit.CompareTag("Obstacle"))
                return true;
        }

        return false;
    }

    public bool CanOccupyCell(Vector2Int cell, GameObject ignoredObject = null)
    {
        return IsCellValid(cell) && !IsCellBlocked(cell, ignoredObject);
    }

    public bool RegisterFloorTile(GridFloorTile tile)
    {
        if (tile == null)
            return false;

        Vector2Int cell = tile.Cell;
        if (!IsWithinBounds(cell))
        {
            Debug.LogWarning($"GridFloorTile '{tile.name}' esta fora dos limites em {cell}.", tile);
            return false;
        }

        if (!ValidateTileGeometry(tile, cell))
            return false;

        if (floorTiles.TryGetValue(cell, out GridFloorTile existing) &&
            existing != null && existing != tile)
        {
            Debug.LogWarning(
                $"Dois GridFloorTiles registrados na celula {cell}: '{existing.name}' e '{tile.name}'.",
                tile
            );
            return false;
        }

        floorTiles[cell] = tile;
        return true;
    }

    public void UnregisterFloorTile(GridFloorTile tile)
    {
        if (tile != null && floorTiles.TryGetValue(tile.Cell, out GridFloorTile current) &&
            current == tile)
            floorTiles.Remove(tile.Cell);
    }

    public bool ValidateCellVisual(GameObject visual, Vector2Int cell, string visualKind)
    {
        if (visual == null)
            return false;

        Vector3 expected = CellToWorld(cell, visual.transform.position.y);
        float alignmentError = Vector2.Distance(
            new Vector2(visual.transform.position.x, visual.transform.position.z),
            new Vector2(expected.x, expected.z)
        );
        Renderer renderer = visual.GetComponentInChildren<Renderer>();
        bool aligned = alignmentError <= validationTolerance;
        bool sized = renderer != null &&
            Mathf.Abs(renderer.bounds.size.x - cellSize) <= validationTolerance &&
            Mathf.Abs(renderer.bounds.size.z - cellSize) <= validationTolerance;

        if (!aligned)
            Debug.LogWarning($"{visualKind} '{visual.name}' desalinhado da celula {cell}.", visual);
        if (!sized)
            Debug.LogWarning($"{visualKind} '{visual.name}' nao ocupa {cellSize}x{cellSize}.", visual);

        return aligned && sized;
    }

    private void RegisterSceneFloorTiles()
    {
        GridFloorTile[] tiles = FindObjectsByType<GridFloorTile>(FindObjectsInactive.Exclude);

        foreach (GridFloorTile tile in tiles)
        {
            if (!tile.IsConfigured)
                tile.Configure(this, WorldToCell(tile.transform.position));

            RegisterFloorTile(tile);
        }
    }

    private void ValidateRegisteredTiles()
    {
        foreach (KeyValuePair<Vector2Int, GridFloorTile> pair in floorTiles)
        {
            if (pair.Value != null)
                ValidateTileGeometry(pair.Value, pair.Key);
        }
    }

    private bool ValidateTileGeometry(GridFloorTile tile, Vector2Int cell)
    {
        Vector3 expected = CellToWorld(cell, tile.transform.position.y);
        float alignmentError = Vector2.Distance(
            new Vector2(tile.transform.position.x, tile.transform.position.z),
            new Vector2(expected.x, expected.z)
        );

        bool aligned = alignmentError <= validationTolerance;
        if (!aligned)
        {
            Debug.LogWarning(
                $"GridFloorTile '{tile.name}' esta desalinhado da celula {cell}.",
                tile
            );
        }

        Renderer renderer = tile.GetComponentInChildren<Renderer>();
        bool sized = renderer != null &&
            Mathf.Abs(renderer.bounds.size.x - cellSize) <= validationTolerance &&
            Mathf.Abs(renderer.bounds.size.z - cellSize) <= validationTolerance;
        if (!sized)
        {
            Debug.LogWarning(
                $"GridFloorTile '{tile.name}' tem tamanho diferente de {cellSize}x{cellSize}.",
                tile
            );
        }

        return aligned && sized;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        float startX = transform.position.x - (width * cellSize) / 2f;
        float startZ = transform.position.z - (height * cellSize) / 2f;

        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(
                new Vector3(startX + x * cellSize, transform.position.y + 0.05f, startZ),
                new Vector3(startX + x * cellSize, transform.position.y + 0.05f, startZ + height * cellSize)
            );
        }

        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(
                new Vector3(startX, transform.position.y + 0.05f, startZ + z * cellSize),
                new Vector3(startX + width * cellSize, transform.position.y + 0.05f, startZ + z * cellSize)
            );
        }
    }
}
