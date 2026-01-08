using UnityEngine;

public class RandomDropoffLocation : MonoBehaviour
{
    [Header("All possible dropoff spots")]
    [SerializeField] private Transform[] dropoffSpots;

    [Header("External Control")]
    [Tooltip("If true, this dropoff will NOT move itself on Start(). A manager/script must control it.")]
    [SerializeField] private bool managedExternally = false;

    [Header("Collision Safety")]
    [Tooltip("Which layers are considered 'blocked' (buildings / walls / sidewalks colliders).")]
    [SerializeField] private LayerMask blockedLayers;

    [Tooltip("Radius used to test if the dropoff point is inside/overlapping a blocked collider.")]
    [SerializeField] private float checkRadius = 0.35f;

    [Tooltip("How many random tries before falling back to scanning all spots.")]
    [SerializeField] private int maxAttempts = 30;

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

        int index = ChooseRandomValidIndex();
        if (index < 0)
        {
            Debug.LogWarning("RandomDropoffLocation: No VALID dropoff spot found (all are blocked).");
            return;
        }

        transform.position = dropoffSpots[index].position;

        // Important when moving colliders/triggers during gameplay
        Physics2D.SyncTransforms();
    }

    // =========================
    // Validity checks
    // =========================

    /// <summary>
    /// Returns true if the given position does NOT overlap blocked colliders.
    /// Uses OverlapCircle to detect if we are inside a building collider.
    /// </summary>
    private bool IsSpotValid(Vector2 pos)
    {
        // If blockedLayers not set, treat all spots as valid (fail-safe)
        if (blockedLayers.value == 0)
            return true;

        Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius, blockedLayers);
        return hit == null;
    }

    private int ChooseRandomValidIndex()
    {
        // Try random picks first
        for (int attempt = 0; attempt < Mathf.Max(1, maxAttempts); attempt++)
        {
            int idx = Random.Range(0, dropoffSpots.Length);

            if (dropoffSpots[idx] == null) continue;
            if (idx == lastIndex) continue;

            Vector2 pos = dropoffSpots[idx].position;
            if (!IsSpotValid(pos)) continue;

            lastIndex = idx;
            return idx;
        }

        // Fallback: scan all spots and take the first valid one
        for (int i = 0; i < dropoffSpots.Length; i++)
        {
            if (dropoffSpots[i] == null) continue;

            Vector2 pos = dropoffSpots[i].position;
            if (!IsSpotValid(pos)) continue;

            lastIndex = i;
            return i;
        }

        return -1;
    }

    // =========================
    // Debug Gizmos (optional)
    // =========================
    private void OnDrawGizmosSelected()
    {
        if (dropoffSpots == null) return;

        Gizmos.color = Color.yellow;
        foreach (var t in dropoffSpots)
        {
            if (t == null) continue;
            Gizmos.DrawWireSphere(t.position, checkRadius);
        }
    }
}
