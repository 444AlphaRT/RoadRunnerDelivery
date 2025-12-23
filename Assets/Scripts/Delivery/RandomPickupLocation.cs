using UnityEngine;

public class RandomPickupLocation : MonoBehaviour
{
    [Header("Possible pickup spots (Pizza, Burger, etc.)")]
    [SerializeField] private Transform[] pickupSpots;

    [Header("Optional: icons that match each spot index")]
    [SerializeField] private GameObject[] spotIcons;

    [Header("Behavior")]
    [SerializeField] private bool avoidRepeatingLastSpot = true;

    private int lastIndex = -1;

    private void Start()
    {
        MoveToRandomSpot();
    }

    public void MoveToRandomSpot()
    {
        if (pickupSpots == null || pickupSpots.Length == 0)
        {
            Debug.LogWarning("RandomPickupLocation: No pickup spots assigned.");
            return;
        }

        int newIndex = ChooseRandomIndex();
        if (newIndex < 0) return;

        ApplyIndex(newIndex);
    }

    public void SetSpotIndex(int index)
    {
        if (pickupSpots == null || pickupSpots.Length == 0)
        {
            Debug.LogWarning("RandomPickupLocation: No pickup spots assigned.");
            return;
        }

        if (index < 0 || index >= pickupSpots.Length)
        {
            Debug.LogWarning($"RandomPickupLocation: Invalid index {index}.");
            return;
        }

        if (pickupSpots[index] == null)
        {
            Debug.LogWarning($"RandomPickupLocation: pickupSpots[{index}] is NULL.");
            return;
        }

        ApplyIndex(index);
    }

    private int ChooseRandomIndex()
    {
        if (pickupSpots.Length == 1)
        {
            if (pickupSpots[0] == null) return -1;
            return 0;
        }

        const int maxAttempts = 25;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int idx = Random.Range(0, pickupSpots.Length);

            if (pickupSpots[idx] == null) continue;
            if (avoidRepeatingLastSpot && idx == lastIndex) continue;

            return idx;
        }

        for (int i = 0; i < pickupSpots.Length; i++)
        {
            if (pickupSpots[i] != null) return i;
        }

        return -1;
    }

    private void ApplyIndex(int index)
    {
        lastIndex = index;
        transform.position = pickupSpots[index].position;
        UpdateIcons(index);
    }

    private void UpdateIcons(int activeIndex)
    {
        if (spotIcons == null || spotIcons.Length == 0) return;

        int count = Mathf.Min(spotIcons.Length, pickupSpots.Length);
        for (int i = 0; i < count; i++)
        {
            if (spotIcons[i] == null) continue;
            spotIcons[i].SetActive(i == activeIndex);
        }
    }
}