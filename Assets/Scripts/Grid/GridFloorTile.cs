using UnityEngine;

public class GridFloorTile : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Vector2Int cell;
    [SerializeField] private bool configured;
    [SerializeField, HideInInspector] private bool bakedByEditorTool;

    public Vector2Int Cell => cell;
    public bool IsConfigured => configured;
    public GridManager GridManager => gridManager;
    public bool WasBakedByEditorTool => bakedByEditorTool;

    public void Configure(
        GridManager owner,
        Vector2Int coordinate,
        bool createdByEditorBake = false)
    {
        gridManager = owner;
        cell = coordinate;
        configured = owner != null;
        bakedByEditorTool = createdByEditorBake;
    }

    void OnEnable()
    {
        if (configured && gridManager != null)
            gridManager.RegisterFloorTile(this);
    }

    void Start()
    {
        if (!configured)
        {
            gridManager = GridManager.Instance;
            if (gridManager != null)
            {
                cell = gridManager.WorldToCell(transform.position);
                configured = true;
                gridManager.RegisterFloorTile(this);
            }
        }
    }

    void OnDisable()
    {
        if (configured && gridManager != null)
            gridManager.UnregisterFloorTile(this);
    }
}
