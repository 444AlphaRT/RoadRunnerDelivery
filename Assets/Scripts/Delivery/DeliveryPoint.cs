using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    public enum PointType
    {
        Pickup,
        Dropoff
    }

    [Header("Delivery Point Type")]
    public PointType pointType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null)
        {
            return; // Not the player, do nothing
        }

        if (pointType == PointType.Pickup)
        {
            player.PickUpPackage();
        }
        else if (pointType == PointType.Dropoff)
        {
            // Only deliver if player actually has a package
            if (!player.HasPackage)
            {
                // Player arrived without a package — ignore
                return;
            }

            player.DeliverPackage();

            // After a successful delivery, move this DropoffPoint to a new random building
            RandomDropoffLocation randomDropoff = GetComponent<RandomDropoffLocation>();
            if (randomDropoff != null)
            {
                randomDropoff.MoveToRandomSpot();
            }
        }
    }
}