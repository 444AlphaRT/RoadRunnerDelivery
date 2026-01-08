using UnityEngine;

public class RandomDropoffLocation : MonoBehaviour
{
    [Header("All possible dropoff spots")]
    [SerializeField] private Transform[] dropoffSpots;

    [Header("External Control")]
    [Tooltip("If true, this dropoff will NOT move itself on Start(). A manager/script must control it.")]
    [SerializeField] private bool managedExternally = false;

    [Header("Collision Safety")]
    [Tooltip("Layers that are NOT allowed to contain the dropoff (buildings, sidewalks, walls, etc).")]
    [SerializeField] private LayerMask blockedLayers;

    [Tooltip("Overlap check radius around the dropoff position.")]
    [SerializeField] private float checkRadius = 0.35f;

    [Tooltip("Extra distance to push the point OUTSIDE the collider (so it sits 'near' the building).")]
    [SerializeField] private float pushOutPadding = 0.15f;

    [Tooltip("How many random tries before scanning all spots.")]
    [SerializeField] private int maxAttempts = 30;

    [Tooltip("How many push-out iterations we do if still overlapping.")]
    [SerializeField] private int maxPushIterations = 5;

    private int lastIndex = -1;

    private void Start()
    {
        if (managedExternally) return;
        MoveToRandomSpot();
    }

    public void MoveToRandomSpot()
    {
        if (dropoffSpots == null || dropoffSpots.Length == 0)
        {
            Debug.LogError("RandomDropoffLocation: No dropoff spots assigned!");
            return;
        }

        int idx = ChooseRandomIndex();
        if (idx < 0) return;

        Vector2 desiredPos = dropoffSpots[idx].position;

        // If the desired spot overlaps blocked colliders, push it outside.
        Vector2 finalPos = ResolveBlockedOverlap(desiredPos);

        transform.position = finalPos;

        // Important when moving colliders/triggers during gameplay
        Physics2D.SyncTransforms();
    }

    private int ChooseRandomIndex()
    {
        if (dropoffSpots.Length == 1)
        {
            if (dropoffSpots[0] == null) return -1;
            lastIndex = 0;
            return 0;
        }

        for (int attempt = 0; attempt < Mathf.Max(1, maxAttempts); attempt++)
        {
            int idx = Random.Range(0, dropoffSpots.Length);
            if (dropoffSpots[idx] == null) continue;
            if (idx == lastIndex) continue;

            lastIndex = idx;
            return idx;
        }

        // Fallback: first valid transform
        for (int i = 0; i < dropoffSpots.Length; i++)
        {
            if (dropoffSpots[i] != null)
            {
                lastIndex = i;
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// If desired position overlaps a blocked collider, push the point to the closest point
    /// on the collider boundary, then a bit outside (padding), so it ends up "near" the building.
    /// </summary>
    private Vector2 ResolveBlockedOverlap(Vector2 desiredPos)
    {
        // If no blocked layers were set, just return desired pos
        if (blockedLayers.value == 0)
            return desiredPos;

        Vector2 pos = desiredPos;

        for (int iter = 0; iter < Mathf.Max(1, maxPushIterations); iter++)
        {
            // Check if we're overlapping something blocked
            Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius, blockedLayers);
            if (hit == null)
                return pos; // already valid

            // Find the closest point on that collider to our position
            Vector2 closest = hit.ClosestPoint(pos);

            // Direction from collider boundary outward to our current pos
            Vector2 dir = (pos - closest);

            // If we're exactly at closest point (can happen), choose a stable direction
            if (dir.sqrMagnitude < 0.0001f)
            {
                // Push away from collider center as a fallback direction
                Vector2 center = hit.bounds.center;
                dir = (pos - center);
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector2.right; // final fallback
            }

            dir.Normalize();

            // Push outside the collider boundary by radius + padding
            pos = closest + dir * (checkRadius + pushOutPadding);
        }

        // If still overlapping after iterations, just return the last attempt
        return pos;
    }

    // Optional debug: show check radius around current dropoff position
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
