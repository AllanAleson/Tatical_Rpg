using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    public float speed = 4f;

    private bool isMoving = false;

    public void MoveAlongPath(List<Vector2Int> path)
    {
        UnitStats stats = GetComponent<UnitStats>();

        if (stats == null)
        {
            Debug.LogWarning("UnitMovement sem UnitStats: " + gameObject.name);
            return;
        }

        if (stats.isDowned)
            return;

        if (!isMoving && path != null && path.Count > 0)
            StartCoroutine(MoveRoutine(path));
    }

    private IEnumerator MoveRoutine(List<Vector2Int> path)
    {
        isMoving = true;

        foreach (Vector2Int cell in path)
        {
            Vector3 targetPosition = new Vector3(cell.x, 0.5f, cell.y);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );

                yield return null;
            }

            transform.position = targetPosition;
        }

        UnitStats stats = GetComponent<UnitStats>();

        if (stats != null)
            stats.SpendMovePoints(path.Count);

        isMoving = false;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}
