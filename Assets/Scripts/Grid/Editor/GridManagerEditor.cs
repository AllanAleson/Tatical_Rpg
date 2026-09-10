using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private const string FloorRootName = "GridFloorTiles";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Bake Floor Tiles Into Scene"))
                BakeFloorTiles((GridManager)target);
        }

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Floor tiles can only be baked outside Play Mode.", MessageType.Info);
    }

    private static void BakeFloorTiles(GridManager manager)
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Floor tiles can only be baked outside Play Mode.", manager);
            return;
        }

        if (manager.width != 20 || manager.height != 20)
        {
            EditorUtility.DisplayDialog(
                "Bake Floor Tiles",
                $"The tactical scene must use a 20x20 grid. Current size: {manager.width}x{manager.height}.",
                "OK");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Bake Floor Tiles Into Scene");

        RemovePreviouslyBakedTiles(manager);

        GameObject root = new GameObject(FloorRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create baked floor root");
        root.transform.SetParent(manager.transform, false);

        Material floorMaterial = null;
        if (manager.legacyFloorSource != null)
        {
            Renderer sourceRenderer = manager.legacyFloorSource.GetComponent<Renderer>();
            if (sourceRenderer != null)
            {
                floorMaterial = sourceRenderer.sharedMaterial;
                Undo.RecordObject(sourceRenderer, "Disable legacy floor renderer");
                sourceRenderer.enabled = false;
                EditorUtility.SetDirty(sourceRenderer);
            }

            Collider sourceCollider = manager.legacyFloorSource.GetComponent<Collider>();
            if (sourceCollider != null)
            {
                Undo.RecordObject(sourceCollider, "Disable legacy floor collider");
                sourceCollider.enabled = false;
                EditorUtility.SetDirty(sourceCollider);
            }
        }

        int created = 0;
        for (int x = manager.MinCellX; x < manager.MinCellX + manager.width; x++)
        {
            for (int z = manager.MinCellZ; z < manager.MinCellZ + manager.height; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(tileObject, "Create baked floor tile");
                tileObject.name = $"GridFloorTile {cell.x},{cell.y}";
                tileObject.layer = manager.floorLayer;
                tileObject.transform.SetParent(root.transform, false);
                tileObject.transform.position = manager.CellToWorld(
                    cell,
                    manager.transform.position.y - manager.floorTileThickness * 0.5f);
                tileObject.transform.localScale = new Vector3(
                    manager.cellSize,
                    manager.floorTileThickness,
                    manager.cellSize);

                Renderer renderer = tileObject.GetComponent<Renderer>();
                if (renderer != null && floorMaterial != null)
                    renderer.sharedMaterial = floorMaterial;

                // The collider is retained only so the existing pointer raycast can hit
                // a cell. GridFloorTile registration remains pathfinding authority.
                GridFloorTile tile = Undo.AddComponent<GridFloorTile>(tileObject);
                tile.Configure(manager, cell, true);
                EditorUtility.SetDirty(tile);
                created++;
            }
        }

        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Baked {created} GridFloorTiles into '{manager.gameObject.scene.name}'.", manager);
    }

    private static void RemovePreviouslyBakedTiles(GridManager manager)
    {
        GridFloorTile[] tiles = Object.FindObjectsByType<GridFloorTile>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (GridFloorTile tile in tiles)
        {
            if (tile != null && tile.WasBakedByEditorTool && tile.GridManager == manager)
                Undo.DestroyObjectImmediate(tile.gameObject);
        }

        for (int i = manager.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = manager.transform.GetChild(i);
            if (child.name == FloorRootName && child.childCount == 0)
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }
}
