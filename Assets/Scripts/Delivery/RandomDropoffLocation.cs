using UnityEngine;

public class RandomDropoffLocation : MonoBehaviour
{
    [Header("All possible dropoff spots")]
    [SerializeField] private Transform[] dropoffSpots;

    [Header("External Control")]
    [Tooltip("If true, this dropoff will NOT move itself on Start(). A manager/script must control it.")]
    [SerializeField] private bool managedExternally = false;

    private int lastIndex = -1;

    private void Start()
    {
        // If another system controls this dropoff (e.g., DeliveryPoint / Stage 3),
        // do NOT auto-randomize here.
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

        int index = ChooseRandomIndex();
        if (index < 0) return;

        transform.position = dropoffSpots[index].position;

        // Important when moving colliders/triggers during gameplay
        Physics2D.SyncTransforms();
    }

    private int ChooseRandomIndex()
    {
        if (dropoffSpots.Length == 1)
        {
            if (dropoffSpots[0] == null) return -1;
            return 0;
        }

        const int maxAttempts = 25;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int idx = Random.Range(0, dropoffSpots.Length);

            if (dropoffSpots[idx] == null) continue;
            if (idx == lastIndex) continue;

            lastIndex = idx;
            return idx;
        }

        // Fallback: first valid spot
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
}
