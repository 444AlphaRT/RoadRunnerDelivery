using UnityEngine;

public class DualPickupManager : MonoBehaviour
{
    [Header("Pickup Points (the objects that will move)")]
    [SerializeField] private RandomPickupLocation pickupA;
    [SerializeField] private RandomPickupLocation pickupB;

    [Header("Same shared spot list for both pickups")]
    [SerializeField] private Transform[] pickupSpots;

    [Header("Optional: prevent repeating last pair")]
    [SerializeField] private bool avoidRepeatingLastPair = true;

    private int lastIndexA = -1;
    private int lastIndexB = -1;

    private void Start()
    {
        // Make sure both pickups use the exact same spot list
        if (pickupA != null) pickupA.SetSpots(pickupSpots);
        if (pickupB != null) pickupB.SetSpots(pickupSpots);

        RespawnBothPickups();
    }

    public void RespawnBothPickups()
    {
        if (pickupA == null || pickupB == null)
        {
            Debug.LogWarning("DualPickupManager: pickupA or pickupB reference is missing.");
            return;
        }

        if (pickupSpots == null || pickupSpots.Length < 2)
        {
            Debug.LogWarning("DualPickupManager: Need at least 2 pickup spots for two pickups.");
            return;
        }

        // Pick 2 different indices
        int idxA = PickIndexExcluding(-1, avoidRepeatingLastPair ? lastIndexA : -1);
        int idxB = PickIndexExcluding(idxA, avoidRepeatingLastPair ? lastIndexB : -1);

        if (idxA < 0 || idxB < 0)
        {
            Debug.LogWarning("DualPickupManager: Failed to pick valid indices for both pickups.");
            return;
        }

        // Move + icon update (RandomPickupLocation is the source of truth)
        pickupA.SetSpotIndex(idxA);
        pickupB.SetSpotIndex(idxB);

        lastIndexA = idxA;
        lastIndexB = idxB;

        // IMPORTANT FIX:
        // Re-enable pickup visuals (markers) after respawn.
        ResetPickupVisuals(pickupA);
        ResetPickupVisuals(pickupB);

        // Good practice after moving transforms that affect colliders/triggers
        Physics2D.SyncTransforms();
    }

    private void ResetPickupVisuals(RandomPickupLocation pickup)
    {
        if (pickup == null) return;

        DeliveryPoint dp = pickup.GetComponent<DeliveryPoint>();
        if (dp != null)
        {
            dp.ResetPickupVisuals();
        }
    }

    private int PickIndexExcluding(int mustNotBe, int alsoPreferNotBe)
    {
        const int maxAttempts = 30;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int idx = Random.Range(0, pickupSpots.Length);

            if (pickupSpots[idx] == null) continue;
            if (idx == mustNotBe) continue;

            // Try to avoid repeating the same index as last time (optional)
            if (alsoPreferNotBe != -1 && idx == alsoPreferNotBe && pickupSpots.Length > 2)
                continue;

            return idx;
        }

        // Fallback: first valid index that is not mustNotBe
        for (int i = 0; i < pickupSpots.Length; i++)
        {
            if (pickupSpots[i] == null) continue;
            if (i == mustNotBe) continue;
            return i;
        }

        return -1;
    }
}
