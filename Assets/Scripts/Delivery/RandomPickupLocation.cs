using UnityEngine;

public class RandomPickupLocation : MonoBehaviour
{
    [Header("Possible pickup spots (Pizza, Burger, etc.)")]
    public Transform[] pickupSpots;

    private void Start()
    {
        // Choose an initial spot when the game starts
        MoveToRandomSpot();
    }

    public void MoveToRandomSpot()
    {
        if (pickupSpots == null || pickupSpots.Length == 0)
        {
            Debug.LogWarning("RandomPickupLocation: No pickup spots assigned.");
            return;
        }

        int index = Random.Range(0, pickupSpots.Length);
        Transform chosenSpot = pickupSpots[index];

        // Move this PickupPoint object to the chosen spot
        transform.position = chosenSpot.position;
    }
}
