using UnityEngine;

public class RandomPickupLocation : MonoBehaviour
{
    [Header("Possible pickup spots (Pizza, Burger, etc.)")]
    [SerializeField] private Transform[] pickupSpots;   // PizzaSpot, BurgerSpot, ...

    [Header("Optional: icons that match each spot index")]
    [SerializeField] private GameObject[] spotIcons;    // PizzaIcon, BurgerIcon, ...

    private int lastIndex = -1; // Remember last chosen index

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

        int newIndex;

        // If there is only one spot, always use it
        if (pickupSpots.Length == 1)
        {
            newIndex = 0;
        }
        else
        {
            // Choose an index that is different from lastIndex
            newIndex = Random.Range(0, pickupSpots.Length - 1);

            // If we hit or pass lastIndex, shift by one
            if (lastIndex != -1 && newIndex >= lastIndex)
            {
                newIndex++;
            }
        }

        lastIndex = newIndex;
        Transform chosenSpot = pickupSpots[newIndex];

        // Move this PickupPoint to the chosen spot
        transform.position = chosenSpot.position;

        // Optional: turn on only the icon that matches the chosen spot
        if (spotIcons != null && spotIcons.Length == pickupSpots.Length)
        {
            for (int i = 0; i < spotIcons.Length; i++)
            {
                if (spotIcons[i] != null)
                {
                    spotIcons[i].SetActive(i == newIndex);
                }
            }
        }
    }
}