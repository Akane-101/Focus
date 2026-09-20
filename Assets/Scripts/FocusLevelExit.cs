using UnityEngine;

public static class FocusLevelExit
{
    public const KeyCode EnterKey = KeyCode.F;

    public static bool IsPlayerAt(Transform door, Transform player, float enterDistance)
    {
        if (door == null || player == null)
        {
            return false;
        }

        Vector2 playerPos = player.position;
        Collider2D doorCollider = door.GetComponent<Collider2D>();

        if (doorCollider != null)
        {
            Vector2 closest = doorCollider.ClosestPoint(playerPos);
            if (Vector2.Distance(playerPos, closest) <= enterDistance)
            {
                return true;
            }

            Bounds bounds = doorCollider.bounds;
            float dx = Mathf.Abs(playerPos.x - bounds.center.x) - bounds.extents.x;
            float dy = Mathf.Abs(playerPos.y - bounds.center.y) - bounds.extents.y;
            return dx <= enterDistance && dy <= enterDistance * 2f;
        }

        return Vector2.Distance(playerPos, (Vector2)door.position) <= enterDistance;
    }

    public static void TryEnter(FocusLevelState levelState, Transform door, float enterDistance, bool hardConditionMet)
    {
        if (levelState == null || door == null || !hardConditionMet || !Input.GetKeyDown(EnterKey))
        {
            return;
        }

        if (!IsPlayerAt(door, levelState.PlayerTransform, enterDistance))
        {
            return;
        }

        levelState.CompleteLevel();
    }
}
