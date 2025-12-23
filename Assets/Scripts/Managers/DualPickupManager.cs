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
        RespawnBothPickups();
    }

    public void RespawnBothPickups()
    {
        if (pickupSpots == null || pickupSpots.Length < 2)
        {
            Debug.LogWarning("DualPickupManager: Need at least 2 pickup spots for two pickups.");
            return;
        }

        // Pick 2 different indices
        int idxA = PickIndexExcluding(-1, avoidRepeatingLastPair ? lastIndexA : -1);
        int idxB = PickIndexExcluding(idxA, avoidRepeatingLastPair ? lastIndexB : -1);

        // Move pickup objects and update their icons
        Apply(pickupA, idxA);
        Apply(pickupB, idxB);

        lastIndexA = idxA;
        lastIndexB = idxB;
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

    private void Apply(RandomPickupLocation pickup, int index)
    {
        if (pickup == null)
        {
            Debug.LogWarning("DualPickupManager: Pickup reference is missing!");
            return;
        }

        if (index < 0 || index >= pickupSpots.Length || pickupSpots[index] == null)
        {
            Debug.LogWarning("DualPickupManager: Invalid pickup spot index.");
            return;
        }

        // Move pickup object
        pickup.transform.position = pickupSpots[index].position;

        // Tell pickup to show the correct icon (new method below)
        pickup.SetSpotIndex(index);
    }
}