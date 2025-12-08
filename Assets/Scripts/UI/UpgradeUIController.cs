using UnityEngine;

public class UpgradeUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject upgradeButton;   // The upgrade button in the HUD

    [Header("Unlock Settings")]
    [SerializeField] private int deliveriesToUnlock = 1; // How many deliveries before the button can be used

    private void Start()
    {
        // Make sure the button starts hidden
        if (upgradeButton != null)
        {
            upgradeButton.SetActive(false);
        }
        else
        {
            Debug.LogError("UpgradeUIController: Upgrade button reference is missing.");
        }
    }

    // Called when a new delivery starts (player picked up a package)
    public void OnPickupStarted(int totalDeliveries)
    {
        if (upgradeButton == null)
            return;

        // If upgrades are unlocked, hide the button while a delivery is active
        if (totalDeliveries >= deliveriesToUnlock)
        {
            upgradeButton.SetActive(false);
        }
    }

    // Called when a delivery is completed (player dropped off a package)
    public void OnDeliveryCompleted(int totalDeliveries)
    {
        if (upgradeButton == null)
            return;

        // If upgrades are unlocked, show the button between deliveries
        if (totalDeliveries >= deliveriesToUnlock)
        {
            upgradeButton.SetActive(true);
        }
    }
}